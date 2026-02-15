import { apiFetch, setToken } from "./api.js";

export async function login(usernameOrEmail, password) {
  const data = await apiFetch("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ usernameOrEmail, password }),
  });

  setToken(data.accessToken);
  return data;
}

export async function me() {
  return apiFetch("/api/auth/me");
}
