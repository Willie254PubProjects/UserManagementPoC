const endpoints = [
  { label: "Workflow — CardPrinting.Create", url: "/api/sample/CardPrinting/Create", note: "[AuthorizeWorkflow]" },
  { label: "Workflow — CardPrinting.Create (scoped)", url: "/api/sample/CardPrinting/Create?bank=KE&branch=0001", note: "[AuthorizeWorkflow] + resource scope" },
  { label: "Permission — CardPrinting.Create", url: "/api/sample/permission-check", note: "[AuthorizeAllPermissions]" },
  { label: "Role — Administrator only", url: "/api/sample/admin-only", note: "[AuthorizeAnyRole]" },
  { label: "Roles — any of Administrator/Manager", url: "/api/sample/roles/any-of", note: "[AuthorizeAnyRole]" },
  { label: "Roles — all of Administrator", url: "/api/sample/roles/all-of", note: "[AuthorizeAllRoles]" },
  { label: "Permissions — any of CardPrinting.Create/CardPrinting.Approve", url: "/api/sample/permissions/any-of", note: "[AuthorizeAnyPermission]" },
  { label: "Permissions — all of CardPrinting.Create/CardRequest.View", url: "/api/sample/permissions/all-of", note: "[AuthorizeAllPermissions]" },
  { label: "Permissions — custom policy", url: "/api/sample/permissions/custom-policy", note: "[AuthRequirement]" },
  { label: "Permissions — combined role + permission", url: "/api/sample/permissions/combined", note: "[AuthorizeAnyRole] + [AuthorizeAnyPermission]" }
];

function showError(message) {
  const box = document.getElementById("error-box");
  document.getElementById("error-text").textContent = message;
  box.classList.remove("d-none");
}

function errorMessage(error, reason) {
  if (error === "access_denied" && reason === "no_matching_user")
    return "Sign-in failed: your Microsoft Entra account is not linked to a user in this system.";
  if (error === "access_denied" && reason === "return_url_not_permitted")
    return "Sign-in failed: the requested return address is not permitted.";
  return `Sign-in failed: ${error}${reason ? " (" + reason + ")" : ""}`;
}

async function callMe() {
  try {
    const res = await fetch("/api/sample/me");
    const body = res.ok ? await res.json() : null;
    const user = res.ok && body?.data ? body.data : null;
    const signedInCard = document.getElementById("signed-in");
    const signedOutCard = document.getElementById("signed-out");
    const signout = document.getElementById("btn-signout");
    if (user) {
      document.getElementById("me-name").textContent = user.displayName || user.userName || "-";
      document.getElementById("me-email").textContent = user.email || "-";
      document.getElementById("me-bank").textContent = `${user.bankId || "-"} / ${user.branchId || "-"}`;
      signedInCard.classList.remove("d-none");
      signedOutCard.classList.add("d-none");
      signout.classList.remove("d-none");
    } else {
      signedInCard.classList.add("d-none");
      signedOutCard.classList.remove("d-none");
      signout.classList.add("d-none");
    }
    const params = new URLSearchParams(window.location.search);
    const error = params.get("error");
    if (error) showError(errorMessage(error, params.get("reason")));
  } catch (err) {
    showError("Could not reach the consumer API: " + String(err));
  }
}

async function runEndpoint(idx) {
  const ep = endpoints[idx];
  const tr = document.querySelector(`tr[data-idx="${idx}"]`);
  const badge = tr.querySelector(".status-badge");
  const msg = tr.querySelector(".result-msg");
  badge.textContent = "...";
  badge.className = "badge status-badge text-bg-secondary";
  msg.textContent = "";
  try {
    const res = await fetch(ep.url);
    const raw = await res.text();
    let text = raw;
    if (raw) {
      try {
        const json = JSON.parse(raw);
        if (res.status >= 200 && res.status < 300) {
          text = json?.data?.message || "OK";
        } else {
          text = JSON.stringify(json, null, 2);
        }
      } catch { /* non-JSON body, keep raw */ }
    }
    const ok = res.status >= 200 && res.status < 300;
    badge.textContent = res.status;
    badge.className = "badge status-badge " + (ok ? "text-bg-success" : "text-bg-danger");
    if (!text && res.status === 401) text = "Not signed in or session expired";
    else if (!text && res.status === 403) text = "Denied — missing role, permission, or scope";
    else if (!text) text = ok ? "OK" : "Request failed";
    msg.textContent = text;
  } catch (err) {
    badge.textContent = "ERR";
    badge.className = "badge status-badge text-bg-danger";
    msg.textContent = String(err);
  }
}

function renderEndpoints() {
  const tbody = document.getElementById("endpoint-body");
  endpoints.forEach((ep, i) => {
    const tr = document.createElement("tr");
    tr.dataset.idx = i;
    tr.innerHTML = `
      <td>${ep.label}<div class="small text-muted">${ep.note}</div></td>
      <td><code class="small">${ep.url}</code></td>
      <td><button class="btn btn-sm btn-outline-primary" onclick="runEndpoint(${i})">Run</button></td>
      <td><span class="badge status-badge text-bg-secondary">-</span></td>
      <td class="result-msg small"></td>`;
    tbody.appendChild(tr);
  });
}

renderEndpoints();
callMe();

function buildQuery(params) {
  const parts = [];
  for (const key in params) {
    const value = params[key];
    if (value === undefined || value === null || value === "") continue;
    if (Array.isArray(value)) {
      value.forEach(v => { if (v) parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(v)}`); });
    } else {
      parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(value)}`);
    }
  }
  return parts.length ? "?" + parts.join("&") : "";
}

function scopeParams() {
  return {
    bank: document.getElementById("dyn-bank").value.trim(),
    branch: document.getElementById("dyn-branch").value.trim()
  };
}

async function runDynamic(kind) {
  const badge = document.getElementById(`dyn-${kind}-status`);
  const msg = document.getElementById(`dyn-${kind}-result`);
  badge.textContent = "...";
  badge.className = "badge status-badge text-bg-secondary";
  msg.textContent = "";
  let url = "";
  if (kind === "workflow") {
    const wf = document.getElementById("dyn-wf").value.trim() || "CardPrinting";
    const action = document.getElementById("dyn-action").value.trim() || "Create";
    url = `/api/sample/${encodeURIComponent(wf)}/${encodeURIComponent(action)}` + buildQuery(scopeParams());
  } else {
    const op = document.getElementById(`dyn-${kind}-op`).value;
    const values = document.getElementById(`dyn-${kind}-values`).value.split(",").map(s => s.trim()).filter(Boolean);
    const field = kind === "roles" ? "role" : "permission";
    const q = {};
    q.operator = op;
    q[field] = values;
    Object.assign(q, scopeParams());
    url = `/api/sample/${kind}/dynamic` + buildQuery(q);
  }
  try {
    const res = await fetch(url);
    const raw = await res.text();
    const ok = res.status >= 200 && res.status < 300;
    let text = raw;
    if (raw) {
      try {
        const json = JSON.parse(raw);
        const d = json?.data;
        text = (d && (d.isAllowed !== undefined))
          ? `${d.isAllowed ? "Allowed" : "Denied"}${d.reason ? " — " + d.reason : ""}`
          : JSON.stringify(json, null, 2);
      } catch { /* non-JSON body, keep raw */ }
    }
    badge.textContent = res.status;
    badge.className = "badge status-badge " + (ok ? "text-bg-success" : "text-bg-danger");
    if (!text) text = ok ? "OK" : "Request failed";
    msg.textContent = text;
  } catch (err) {
    badge.textContent = "ERR";
    badge.className = "badge status-badge text-bg-danger";
    msg.textContent = String(err);
  }
}