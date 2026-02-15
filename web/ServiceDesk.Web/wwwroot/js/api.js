const API_BASE = "http://localhost:5030";

export function setToken(token) {
  localStorage.setItem("accessToken", token);
}

export function getToken() {
  return localStorage.getItem("accessToken");
}

export function clearToken() {
  localStorage.removeItem("accessToken");
}

// ✅ parse seguro (JSON o texto)
async function readResponse(res) {
  const ct = (res.headers.get("content-type") || "").toLowerCase();
  const text = await res.text();

  // Si viene HTML (por ejemplo login/redirect), lo detectamos
  if (ct.includes("text/html")) {
    return { kind: "html", data: text };
  }

  if (ct.includes("application/json")) {
    try {
      return { kind: "json", data: text ? JSON.parse(text) : null };
    } catch {
      return { kind: "text", data: text };
    }
  }

  // otros content-types
  try {
    return { kind: "json", data: text ? JSON.parse(text) : null };
  } catch {
    return { kind: "text", data: text };
  }
}

export async function apiFetch(path, options = {}) {
  const token = getToken();

  const headers = {
    Accept: "application/json",
    ...(options.headers || {}),
  };

  // ✅ Solo poner Content-Type si realmente enviamos body
  const hasBody = options.body !== undefined && options.body !== null;
  if (hasBody && !headers["Content-Type"] && !headers["content-type"]) {
    headers["Content-Type"] = "application/json";
  }

  // ✅ Bearer token si existe
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
    // ✅ si en algún lado usas cookies/sesión, esto evita problemas
    credentials: "include",
  });

  if (res.status === 401) {
    clearToken();
    // opcional: redirigir al login
    // window.location.href = "/Auth/Login";
  }

  const parsed = await readResponse(res);

  if (!res.ok) {
    // error más informativo
    if (parsed.kind === "json") {
      const msg =
        parsed.data?.error ||
        parsed.data?.message ||
        parsed.data?.detail ||
        `HTTP ${res.status}`;
      throw new Error(msg);
    }

    if (parsed.kind === "html") {
      throw new Error(
        `HTTP ${res.status} (HTML devuelto). Probable redirect/login.`
      );
    }

    throw new Error(`HTTP ${res.status}: ${String(parsed.data || "")}`.trim());
  }

  // OK
  if (parsed.kind === "json") return parsed.data;

  // Si era texto (raro), devolvemos igual
  return parsed.data;
}
