// C:\ProyectoASPNET\servicedesk\web\ServiceDesk.Web\wwwroot\js\tickets.js
(() => {
  const form = document.querySelector('form[data-tickets-filter="1"]');
  const tbody = document.querySelector('tbody[data-tickets-body="1"]');
  const pager = document.querySelector('[data-tickets-pager="1"]');
  const alertBox = document.querySelector('[data-tickets-alert="1"]');
  const loading = document.querySelector('[data-tickets-loading="1"]');
  const totalEl = document.querySelector('[data-tickets-total="1"]');

  // Hidden inputs (en tu Index.cshtml son INPUT HIDDEN, no selects)
  const sortBySelect = document.getElementById("sortBySelect");
  const sortDirSelect = document.getElementById("sortDirSelect");

  // Delete modal
  const deleteModalEl = document.getElementById("deleteTicketModal");
  const deleteTicketIdEl = document.getElementById("deleteTicketId");
  const deleteReasonEl = document.getElementById("deleteReasonInput");
  const deleteTitleEl = document.querySelector('[data-delete-title="1"]');
  const deleteErrEl = document.getElementById("deleteTicketError");
  const confirmDeleteBtn = document.getElementById("confirmDeleteTicketBtn");

  // Sort headers
  const sortHeaderLinks = document.querySelectorAll("[data-sort-by]");
  const sortIndicators = document.querySelectorAll("[data-sort-ind]");

  if (!form || !tbody || !pager) return;

  // MVC endpoints
  const API_BASE = "/Tickets/AjaxList";
  const WEB_DELETE_ENDPOINT = "/Tickets/Delete"; // POST /Tickets/Delete/{id}

  // =========================
  // Helpers UI
  // =========================
  function setAlert(type, msg) {
    if (!alertBox) return;
    if (!msg) {
      alertBox.classList.add("d-none");
      alertBox.innerHTML = "";
      return;
    }
    alertBox.className = `alert alert-${type}`;
    alertBox.classList.remove("d-none");
    alertBox.innerText = msg;
  }

  function setLoading(isLoading) {
    if (!loading) return;
    loading.classList.toggle("d-none", !isLoading);
  }

  function setDeleteError(msg) {
    if (!deleteErrEl) return;
    if (!msg) {
      deleteErrEl.classList.add("d-none");
      deleteErrEl.textContent = "";
      return;
    }
    deleteErrEl.classList.remove("d-none");
    deleteErrEl.textContent = msg;
  }

  function escapeHtml(str) {
    return String(str ?? "").replace(/[&<>"']/g, (m) => ({
      "&": "&amp;",
      "<": "&lt;",
      ">": "&gt;",
      '"': "&quot;",
      "'": "&#039;",
    }[m]));
  }

  function redirectToLogin() {
    window.location.href =
      "/Auth/Login?returnUrl=" + encodeURIComponent(location.pathname + location.search);
  }

  // ✅ AntiForgeryToken (si lo pones en Index.cshtml dentro del form)
  function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
  }

  async function readError(res) {
    const ct = res.headers.get("content-type") || "";
    if (ct.includes("application/json")) {
      const j = await res.json().catch(() => null);
      return j?.message || j?.detail || j?.error || (j ? JSON.stringify(j) : "");
    }
    return await res.text().catch(() => "");
  }

  // =========================
  // Delete modal
  // =========================
  function bindDeleteButtons() {
    document.querySelectorAll('[data-ticket-delete="1"]').forEach(btn => {
      btn.addEventListener("click", () => {
        const id = btn.dataset.ticketId;
        const title = btn.dataset.ticketTitle || "—";
        if (!id) return;

        if (deleteTicketIdEl) deleteTicketIdEl.value = id;
        if (deleteReasonEl) deleteReasonEl.value = "";
        if (deleteTitleEl) deleteTitleEl.textContent = title;
        setDeleteError("");

        if (deleteModalEl && window.bootstrap) {
          const modal = window.bootstrap.Modal.getOrCreateInstance(deleteModalEl);
          modal.show();
        }
      });
    });
  }

  // =========================
  // Mappers
  // =========================
  function statusToText(s) {
    if (typeof s === "string") {
      const v = s.trim();
      if (v === "Open" || v === "InProgress" || v === "Closed") return v;
      const n = Number(v);
      if (Number.isFinite(n)) s = n;
      else return "Open";
    }

    const n = Number(s);
    if (!Number.isFinite(n)) return "Open";

    if ((n & 4) === 4) return "Closed";
    if ((n & 2) === 2) return "InProgress";
    if ((n & 1) === 1) return "Open";
    return "Open";
  }

  function priorityToText(p) {
    if (typeof p === "string") {
      const v = p.trim();
      if (v === "P1" || v === "P2" || v === "P3") return v;
      const n = Number(v);
      if (Number.isFinite(n)) p = n;
      else return "P3";
    }

    const n = Number(p);
    if (!Number.isFinite(n)) return "P3";
    if (n === 1) return "P1";
    if (n === 2) return "P2";
    return "P3";
  }

  function badgeStatus(statusAny) {
    const status = statusToText(statusAny);
    if (status === "Closed") return `<span class="badge bg-success">Cerrado</span>`;
    if (status === "InProgress") return `<span class="badge bg-info text-dark">En progreso</span>`;
    return `<span class="badge bg-warning text-dark">Abierto</span>`;
  }

  function badgePriority(pAny) {
    const p = priorityToText(pAny);
    if (p === "P1") return `<span class="badge bg-danger">P1</span>`;
    if (p === "P2") return `<span class="badge bg-warning text-dark">P2</span>`;
    return `<span class="badge bg-secondary">P3</span>`;
  }

  // =========================
  // Fechas
  // =========================
  function normalizeIsoToDate(iso) {
    if (!iso) return null;
    const s = String(iso);

    if (s.endsWith("Z") || /[+-]\d{2}:\d{2}$/.test(s)) {
      const d = new Date(s);
      return isNaN(d.getTime()) ? null : d;
    }

    const d = new Date(s + "Z");
    return isNaN(d.getTime()) ? null : d;
  }

  function formatFecha(iso) {
    const d = normalizeIsoToDate(iso);
    if (!d) return "-";
    return d.toLocaleString("es-CL", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit"
    });
  }

  // =========================
  // Close/Reopen (si usas form hidden)
  // =========================
  function postTicketAction(url) {
    const actionForm = document.getElementById("ticket-action-form");
    if (!actionForm) {
      // Si no tienes ese form hidden, igual tu Index.cshtml ya tiene forms por fila,
      // así que no es "obligatorio". Esto solo evita recargar y mantiene la UX.
      window.location.href = url;
      return;
    }
    actionForm.action = url;
    actionForm.submit();
  }

  // =========================
  // Render
  // =========================
  function renderRows(items) {
    if (!items || items.length === 0) {
      tbody.innerHTML = `
        <tr>
          <td colspan="7" class="text-secondary text-center py-4">
            No hay tickets con esos filtros.
          </td>
        </tr>`;
      return;
    }

    tbody.innerHTML = items.map(t => {
      const id = t.id;
      const title = escapeHtml(t.title);
      const createdBy = escapeHtml(t.createdBy);
      const assignedTo = escapeHtml(t.assignedTo ?? "-");
      const updated = formatFecha(t.updatedAtUtc);

      const st = statusToText(t.status);
      const pr = priorityToText(t.priority);

      const isClosed = (st === "Closed");
      const actionBtn = isClosed
        ? `<button type="button" class="btn btn-sm btn-outline-success" data-action="reopen" data-id="${id}">Reabrir</button>`
        : `<button type="button" class="btn btn-sm btn-outline-warning" data-action="close" data-id="${id}">Cerrar</button>`;

      return `
        <tr data-ticket-id="${id}">
          <td>
            <div class="d-flex flex-column">
              <a class="text-decoration-none fw-semibold" href="/Tickets/Details/${id}">
                ${title}
              </a>
              <span class="text-secondary small">ID: ${escapeHtml(String(id).substring(0, 8))}</span>
            </div>
          </td>
          <td>${badgeStatus(st)}</td>
          <td>${badgePriority(pr)}</td>
          <td class="text-nowrap">${createdBy}</td>
          <td class="text-nowrap">${assignedTo}</td>
          <td class="text-nowrap">${escapeHtml(updated)}</td>
          <td class="text-end text-nowrap">
            <div class="d-inline-flex gap-2 flex-wrap justify-content-end">
              <a class="btn btn-sm btn-outline-light" href="/Tickets/Details/${id}">Ver</a>
              <a class="btn btn-sm btn-outline-secondary" href="/Tickets/Edit/${id}">Editar</a>

              <button type="button"
                      class="btn btn-sm btn-outline-danger"
                      data-ticket-delete="1"
                      data-ticket-id="${escapeHtml(id)}"
                      data-ticket-title="${escapeHtml(t.title)}">
                Borrar
              </button>

              ${actionBtn}
            </div>
          </td>
        </tr>
      `;
    }).join("");

    // Close/Reopen
    tbody.querySelectorAll('button[data-action][data-id]').forEach(btn => {
      btn.addEventListener("click", () => {
        const id = btn.dataset.id;
        const action = btn.dataset.action;
        if (!id || !action) return;

        const url = action === "reopen"
          ? `/Tickets/Reopen/${id}`
          : `/Tickets/Close/${id}`;

        postTicketAction(url);
      });
    });

    bindDeleteButtons();
  }

  function renderPager(page, pageSize, total, q) {
    const totalPages = Math.max(1, Math.ceil((total || 0) / (pageSize || 20)));
    const hasPrev = page > 1;
    const hasNext = page < totalPages;

    const makeLink = (p) => {
      const params = new URLSearchParams(q);
      params.set("page", String(p));
      params.set("pageSize", String(pageSize));
      return `?${params.toString()}`;
    };

    pager.innerHTML = `
      <ul class="pagination justify-content-center gap-2">
        <li class="page-item ${hasPrev ? "" : "disabled"}">
          <a class="page-link rounded-3" href="${makeLink(page - 1)}" data-page="${page - 1}">← Anterior</a>
        </li>
        <li class="page-item disabled">
          <span class="page-link rounded-3">Página ${page} de ${totalPages}</span>
        </li>
        <li class="page-item ${hasNext ? "" : "disabled"}">
          <a class="page-link rounded-3" href="${makeLink(page + 1)}" data-page="${page + 1}">Siguiente →</a>
        </li>
      </ul>
    `;

    pager.querySelectorAll("a[data-page]").forEach(a => {
      a.addEventListener("click", (ev) => {
        ev.preventDefault();
        const nextPage = Number(a.dataset.page);
        if (!Number.isFinite(nextPage) || nextPage < 1) return;
        fetchAndRender({ ...q, page: String(nextPage), pageSize: String(pageSize) }, true);
      });
    });
  }

  // =========================
  // Filtros
  // =========================
  function readFiltersFromForm() {
    const fd = new FormData(form);
    const q = {};
    fd.forEach((v, k) => {
      const s = String(v ?? "").trim();
      if (s !== "") q[k] = s;
    });

    if (!q.page) q.page = "1";
    if (!q.pageSize) q.pageSize = "20";

    if (!q.sortBy) q.sortBy = (sortBySelect?.value || "createdAt");
    if (!q.sortDir) q.sortDir = (sortDirSelect?.value || "desc");

    return q;
  }

  function applySortIndicators(sortBy, sortDir) {
    sortIndicators.forEach(el => { el.textContent = ""; });

    const key = String(sortBy || "").toLowerCase();
    const dir = String(sortDir || "desc").toLowerCase();

    const map = {
      "title": "title",
      "status": "status",
      "priority": "priority",
      "createdby": "createdBy",
      "assignedto": "assignedTo",
      "updated": "updatedAt",
      "updatedat": "updatedAt",
      "createdat": "createdAt",
      "created": "createdAt"
    };

    const indKey = map[key] || null;
    if (!indKey) return;

    const ind = document.querySelector(`[data-sort-ind="${indKey}"]`);
    if (!ind) return;

    ind.textContent = dir === "asc" ? "↑" : "↓";
  }

  // ✅ FIX: al cargar, sincroniza hidden inputs con la querystring
  function syncFormFromQuery(q) {
    // inputs hidden en tu vista
    const setVal = (id, key) => {
      const el = document.getElementById(id);
      if (el && q[key] != null && String(q[key]).trim() !== "") el.value = String(q[key]);
    };

    setVal("statusSelect", "status");
    setVal("prioritySelect", "priority");
    setVal("categorySelect", "category");
    setVal("typeSelect", "type");

    setVal("createdFrom", "createdFrom");
    setVal("createdTo", "createdTo");
    setVal("updatedFrom", "updatedFrom");
    setVal("updatedTo", "updatedTo");

    setVal("createdByInput", "createdBy");
    setVal("assignedToInput", "assignedTo");

    setVal("sortBySelect", "sortBy");
    setVal("sortDirSelect", "sortDir");
    setVal("pageSizeSelect", "pageSize");
  }

  // =========================
  // Fetch
  // =========================
  async function fetchAndRender(q, pushState) {
    setAlert("", "");
    setLoading(true);

    try {
      const params = new URLSearchParams(q);
      const url = `${API_BASE}?${params.toString()}`;

      const res = await fetch(url, {
        method: "GET",
        headers: { "Accept": "application/json" },
        // ✅ si en algún momento tu auth usa cookies, esto evita fallos (no rompe si no usas cookies)
        credentials: "same-origin"
      });

      const contentType = res.headers.get("content-type") || "";
      if (contentType.includes("text/html")) {
        redirectToLogin();
        return;
      }

      if (res.status === 401 || res.status === 403) {
        redirectToLogin();
        return;
      }

      if (!res.ok) {
        const text = await readError(res);
        throw new Error(`HTTP ${res.status}: ${text}`);
      }

      const data = await res.json();

      renderRows(data.items || []);
      renderPager(data.page || 1, data.pageSize || 20, data.total || 0, q);

      if (totalEl) totalEl.textContent = String(data.total ?? 0);

      applySortIndicators(q.sortBy, q.sortDir);

      if (pushState) {
        history.pushState(q, "", `?${params.toString()}`);
      }
    } catch (err) {
      setAlert("danger", "No pude cargar tickets. " + (err?.message ?? err));
    } finally {
      setLoading(false);
    }
  }

  // =========================
  // Debounce (auto refresh)
  // =========================
  function debounce(fn, wait) {
    let t = null;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn(...args), wait);
    };
  }

  const autoRefresh = debounce(() => {
    const q = readFiltersFromForm();
    q.page = "1";
    fetchAndRender(q, true);
  }, 350);

  const autoInputs = [
    document.getElementById("statusSelect"),
    document.getElementById("prioritySelect"),
    document.getElementById("categorySelect"),
    document.getElementById("typeSelect"),
    document.getElementById("createdFrom"),
    document.getElementById("createdTo"),
    document.getElementById("updatedFrom"),
    document.getElementById("updatedTo"),
    document.getElementById("createdByInput"),
    document.getElementById("assignedToInput"),
    document.getElementById("sortBySelect"),
    document.getElementById("sortDirSelect"),
    document.getElementById("pageSizeSelect"),
  ].filter(Boolean);

  autoInputs.forEach(el => el.addEventListener("change", () => autoRefresh()));

  const searchInput = document.getElementById("searchInput");
  if (searchInput) searchInput.addEventListener("input", () => autoRefresh());

  form.addEventListener("submit", (ev) => {
    ev.preventDefault();
    const q = readFiltersFromForm();
    q.page = "1";
    fetchAndRender(q, true);
  });

  sortHeaderLinks.forEach(a => {
    a.addEventListener("click", (ev) => {
      ev.preventDefault();
      const by = a.getAttribute("data-sort-by");
      if (!by) return;

      const curBy = (sortBySelect?.value || "createdAt");
      const curDir = (sortDirSelect?.value || "desc");

      if (sortBySelect) sortBySelect.value = by;

      if (by === curBy) {
        if (sortDirSelect) sortDirSelect.value = (String(curDir).toLowerCase() === "asc") ? "desc" : "asc";
      } else {
        if (sortDirSelect) sortDirSelect.value = "desc";
      }

      autoRefresh();
    });
  });

  window.addEventListener("popstate", (ev) => {
    const q = ev.state;
    if (q) {
      syncFormFromQuery(q);
      fetchAndRender(q, false);
    }
  });

  // =========================
  // Delete ticket (modal)
  // =========================
  async function deleteTicketNow() {
    const id = deleteTicketIdEl?.value;
    const reason = (deleteReasonEl?.value || "").trim();

    setDeleteError("");

    if (!id) {
      setDeleteError("Falta ticketId.");
      return;
    }
    if (!reason) {
      setDeleteError("Debes ingresar un motivo.");
      return;
    }

    const payload = { reason };

    try {
      if (confirmDeleteBtn) confirmDeleteBtn.disabled = true;

      const token = getAntiForgeryToken();
      const headers = {
        "Content-Type": "application/json",
        "Accept": "application/json"
      };
      if (token) headers["RequestVerificationToken"] = token;

      const res = await fetch(`${WEB_DELETE_ENDPOINT}/${encodeURIComponent(id)}`, {
        method: "POST",
        headers,
        body: JSON.stringify(payload),
        credentials: "same-origin"
      });

      if (res.status === 401 || res.status === 403) {
        redirectToLogin();
        return;
      }

      if (!res.ok) {
        const txt = await readError(res);
        throw new Error(txt || `HTTP ${res.status}`);
      }

      if (deleteModalEl && window.bootstrap) {
        const modal = window.bootstrap.Modal.getOrCreateInstance(deleteModalEl);
        modal.hide();
      }

      const q = readFiltersFromForm();
      fetchAndRender(q, false);

    } catch (err) {
      setDeleteError("No se pudo borrar. " + (err?.message ?? err));
    } finally {
      if (confirmDeleteBtn) confirmDeleteBtn.disabled = false;
    }
  }

  if (confirmDeleteBtn) confirmDeleteBtn.addEventListener("click", deleteTicketNow);

  // =========================
  // Init
  // =========================
  const initQ = Object.fromEntries(new URLSearchParams(location.search).entries());
  const bootQ = { page: "1", pageSize: "20", sortBy: "createdAt", sortDir: "desc", ...initQ };

  // ✅ importantísimo: sincroniza hidden inputs desde query
  syncFormFromQuery(bootQ);

  applySortIndicators(bootQ.sortBy, bootQ.sortDir);
  fetchAndRender(bootQ, false);

  // bind delete on first render (server-render)
  bindDeleteButtons();

})();
