"use strict";

const state = {
  summary: null,
  methods: [],
  links: [],
  methodById: new Map(),
  domainsById: new Map(),
  linksOut: new Map(),
  linksIn: new Map(),
  filtered: [],
  selectedId: null,
  debounceTimer: 0,
};

const els = {};

function fmt(value) {
  return Number(value || 0).toLocaleString("en-US");
}

function pct(count, total) {
  if (!total) return "0%";
  return `${((count / total) * 100).toFixed(1)}%`;
}

function coverageLabel(value) {
  if (value === "native-owner") return "Native-owner";
  if (value === "system-map") return "System map";
  return "Unmapped";
}

function methodTitle(method) {
  return `${method.type}::${method.name}`;
}

function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

async function loadData() {
  const documents = await Promise.all(["summary", "methods", "links"].map(async (name) => {
    const response = await fetch(`data/${name}.json`);
    if (!response.ok) throw new Error(`Missing ${name} data (${response.status})`);
    const bytes = await response.arrayBuffer();
    return { name, bytes, value: JSON.parse(new TextDecoder().decode(bytes)) };
  }));
  const [summary, methods, links] = documents.map((document) => document.value);
  const generation = summary.meta.generation;
  if (generation) {
    if (generation.status !== "complete" || generation.outputs?.length !== 2) {
      throw new Error("Incomplete generation receipt");
    }
    for (const document of documents.slice(1)) {
      const expected = generation.outputs.find((output) => output.path === `${document.name}.json`);
      const hash = Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", document.bytes)))
        .map((byte) => byte.toString(16).padStart(2, "0")).join("");
      if (!expected || expected.bytes !== document.bytes.byteLength || expected.sha256 !== hash) {
        throw new Error(`Mixed or damaged ${document.name} output`);
      }
    }
  }
  if (summary.meta.methodCount !== methods.length || summary.meta.internalEdgeCount !== links.length) {
    throw new Error("Mixed or incomplete generated data; select one completed baseline.");
  }
  const ids = new Set(methods.map((method) => method.id));
  if (ids.size !== methods.length || links.some((link) => !ids.has(link.source) || !ids.has(link.target))) {
    throw new Error("Generated method/link identities are inconsistent.");
  }

  state.summary = summary;
  state.methods = methods;
  state.links = links;
  state.methodById = new Map(methods.map((method) => [method.id, method]));
  state.domainsById = new Map(summary.domains.map((domain) => [domain.id, domain]));

  for (const link of links) {
    if (!state.linksOut.has(link.source)) state.linksOut.set(link.source, []);
    if (!state.linksIn.has(link.target)) state.linksIn.set(link.target, []);
    state.linksOut.get(link.source).push(link);
    state.linksIn.get(link.target).push(link);
  }

  for (const list of [...state.linksOut.values(), ...state.linksIn.values()]) {
    list.sort((a, b) => b.count - a.count);
  }
}

function initElements() {
  for (const id of [
    "buildLine",
    "methodCount",
    "edgeCount",
    "ownerCoverage",
    "mapCoverage",
    "unmappedCoverage",
    "searchInput",
    "domainSelect",
    "systemSelect",
    "coverageSelect",
    "visibilitySelect",
    "nodeLimit",
    "nodeLimitValue",
    "graphStatus",
    "graphSvg",
    "details",
    "clearSelection",
    "coverageBars",
    "domainBars",
    "systemBars",
    "resultCount",
    "methodRows",
  ]) {
    els[id] = document.getElementById(id);
  }
}

function populateFilters() {
  els.domainSelect.innerHTML = `<option value="">All</option>` + state.summary.domains
    .map((domain) => `<option value="${domain.id}">${domain.id} ${escapeHtml(domain.title)}</option>`)
    .join("");

  els.systemSelect.innerHTML = `<option value="">All</option>` + state.summary.systems
    .map((system) => `<option value="${escapeHtml(system.id)}">${escapeHtml(system.name)}</option>`)
    .join("");

  const visibilityValues = Object.keys(state.summary.meta.visibilityCounts).sort();
  els.visibilitySelect.innerHTML = `<option value="">All</option>` + visibilityValues
    .map((value) => `<option value="${escapeHtml(value)}">${escapeHtml(value)}</option>`)
    .join("");
}

function renderOverview() {
  const meta = state.summary.meta;
  const counts = meta.coverageCounts;
  const total = meta.methodCount;

  els.buildLine.textContent = `${meta.game} | DATA BASELINE ${meta.branch}/${meta.steamBuild} | Assembly SHA-256 ${meta.assemblySha256} | generated ${meta.generatedAtUtc}. Research coverage for this baseline; current game/API support is not implied.`;
  els.methodCount.textContent = fmt(total);
  els.edgeCount.textContent = fmt(meta.internalEdgeCount);
  els.ownerCoverage.textContent = `${fmt(counts["native-owner"])} (${pct(counts["native-owner"], total)})`;
  els.mapCoverage.textContent = `${fmt(counts["system-map"])} (${pct(counts["system-map"], total)})`;
  els.unmappedCoverage.textContent = `${fmt(counts.unmapped)} (${pct(counts.unmapped, total)})`;
}

function barRow(label, value, max, detail, cls = "") {
  const width = max ? Math.max(2, (value / max) * 100) : 0;
  return `
    <div class="bar-row" title="${escapeHtml(label)}">
      <div class="bar-label">${escapeHtml(label)}</div>
      <div>${escapeHtml(detail)}</div>
      <div class="bar-track"><div class="bar-fill ${cls}" style="width:${width}%"></div></div>
    </div>
  `;
}

function renderCharts() {
  const meta = state.summary.meta;
  const counts = meta.coverageCounts;
  const total = meta.methodCount;

  els.coverageBars.innerHTML = `<div class="bar-list">` + [
    barRow("Native-owner report coverage", counts["native-owner"] || 0, total, pct(counts["native-owner"], total), "native-owner"),
    barRow("System map only", counts["system-map"] || 0, total, pct(counts["system-map"], total), "system-map"),
    barRow("Unmapped metadata", counts.unmapped || 0, total, pct(counts.unmapped, total), "unmapped"),
  ].join("") + `</div>`;

  const maxDomain = Math.max(...state.summary.domains.map((domain) => domain.matchedMethodCount), 1);
  els.domainBars.innerHTML = `<div class="bar-list">` + state.summary.domains
    .map((domain) => barRow(`${domain.id} ${domain.title}`, domain.matchedMethodCount, maxDomain, fmt(domain.matchedMethodCount), "native-owner"))
    .join("") + `</div>`;

  const topSystems = state.summary.systems.slice(0, 12);
  const maxSystem = Math.max(...topSystems.map((system) => system.methodCount), 1);
  els.systemBars.innerHTML = `<div class="bar-list">` + topSystems
    .map((system) => barRow(system.name, system.methodCount, maxSystem, `${fmt(system.methodCount)} / ${system.coveragePercent}%`, "system-map"))
    .join("") + `</div>`;
}

function getFilters() {
  return {
    search: els.searchInput.value.trim().toLowerCase(),
    domain: els.domainSelect.value,
    system: els.systemSelect.value,
    coverage: els.coverageSelect.value,
    visibility: els.visibilitySelect.value,
    limit: Number(els.nodeLimit.value),
  };
}

function methodMatches(method, filters) {
  if (filters.coverage && method.coverage !== filters.coverage) return false;
  if (filters.visibility && method.visibility !== filters.visibility) return false;
  if (filters.domain && !method.domains.includes(filters.domain)) return false;
  if (filters.system && !method.systems.includes(filters.system)) return false;

  if (filters.search) {
    const haystack = `${method.type} ${method.name} ${method.fullName} ${method.token} ${method.returnType}`.toLowerCase();
    if (!haystack.includes(filters.search)) return false;
  }

  return true;
}

function scoreMethod(method) {
  const coverageBoost = method.coverage === "native-owner" ? 200000 : method.coverage === "system-map" ? 80000 : 0;
  const changeBoost = method.change ? 50000 : 0;
  return coverageBoost + changeBoost + method.callsIn * 3 + method.callsOut * 2 + method.bodySize;
}

function applyFilters() {
  const filters = getFilters();
  els.nodeLimitValue.value = String(filters.limit);
  els.nodeLimitValue.textContent = String(filters.limit);

  state.filtered = state.methods
    .filter((method) => methodMatches(method, filters))
    .sort((a, b) => scoreMethod(b) - scoreMethod(a));

  renderTable(filters.limit);
  renderGraph(filters.limit);
}

function renderTable(limit) {
  const rows = state.filtered.slice(0, Math.max(limit, 80));
  els.resultCount.textContent = `${fmt(state.filtered.length)} matched`;
  els.methodRows.innerHTML = rows.map((method) => {
    const domains = method.domains.map((id) => id).join(", ");
    const systems = method.systems.slice(0, 3).join(", ");
    return `
      <tr data-id="${method.id}">
        <td><span class="badge ${method.coverage}">${coverageLabel(method.coverage)}</span>${method.change ? ` <span class="badge changed">${method.change}</span>` : ""}</td>
        <td class="mono">${escapeHtml(method.type)}</td>
        <td class="mono">${escapeHtml(method.name)}</td>
        <td>${escapeHtml(systems)}</td>
        <td>${escapeHtml(domains)}</td>
        <td>${fmt(method.callsIn)} in / ${fmt(method.callsOut)} out</td>
      </tr>
    `;
  }).join("");

  els.methodRows.querySelectorAll("tr").forEach((row) => {
    row.addEventListener("click", () => selectMethod(Number(row.dataset.id)));
  });
}

function graphNodes(limit) {
  if (state.selectedId !== null) {
    const ids = new Set([state.selectedId]);
    const incoming = (state.linksIn.get(state.selectedId) || []).slice(0, Math.floor(limit / 2));
    const outgoing = (state.linksOut.get(state.selectedId) || []).slice(0, Math.floor(limit / 2));
    for (const link of incoming) ids.add(link.source);
    for (const link of outgoing) ids.add(link.target);
    return [...ids].map((id) => state.methodById.get(id)).filter(Boolean);
  }

  return state.filtered.slice(0, limit);
}

function assignPositions(nodes, width, height) {
  const selected = state.selectedId !== null ? state.methodById.get(state.selectedId) : null;
  const positions = new Map();

  if (selected && nodes.some((node) => node.id === selected.id)) {
    positions.set(selected.id, { x: width * 0.5, y: height * 0.5 });
    const others = nodes.filter((node) => node.id !== selected.id);
    const incomingIds = new Set((state.linksIn.get(selected.id) || []).map((link) => link.source));
    const outgoingIds = new Set((state.linksOut.get(selected.id) || []).map((link) => link.target));
    const left = others.filter((node) => incomingIds.has(node.id) && !outgoingIds.has(node.id));
    const right = others.filter((node) => outgoingIds.has(node.id) && !incomingIds.has(node.id));
    const both = others.filter((node) => incomingIds.has(node.id) && outgoingIds.has(node.id));
    placeArc(left, width * 0.26, height * 0.5, Math.min(height * 0.4, width * 0.18), Math.PI * 0.65, Math.PI * 1.35, positions);
    placeArc(right, width * 0.74, height * 0.5, Math.min(height * 0.4, width * 0.18), -Math.PI * 0.35, Math.PI * 0.35, positions);
    placeArc(both, width * 0.5, height * 0.22, Math.min(height * 0.16, width * 0.2), Math.PI, Math.PI * 2, positions);
    return positions;
  }

  const columns = Math.max(8, Math.ceil(Math.sqrt(nodes.length * (width / height))));
  const rowHeight = Math.max(32, (height - 60) / Math.ceil(nodes.length / columns));
  const colWidth = (width - 60) / columns;
  nodes.forEach((node, index) => {
    const col = index % columns;
    const row = Math.floor(index / columns);
    positions.set(node.id, {
      x: 30 + colWidth * col + colWidth * 0.5,
      y: 34 + rowHeight * row + rowHeight * 0.5,
    });
  });

  return positions;
}

function placeArc(nodes, centerX, centerY, radius, start, end, positions) {
  if (!nodes.length) return;
  const step = nodes.length === 1 ? 0 : (end - start) / (nodes.length - 1);
  nodes.forEach((node, index) => {
    const angle = nodes.length === 1 ? (start + end) / 2 : start + step * index;
    positions.set(node.id, {
      x: centerX + Math.cos(angle) * radius,
      y: centerY + Math.sin(angle) * radius,
    });
  });
}

function nodeRadius(method) {
  return Math.max(4, Math.min(13, 4 + Math.sqrt(method.callsIn + method.callsOut) * 0.45));
}

function renderGraph(limit) {
  const svg = els.graphSvg;
  const width = svg.clientWidth || 900;
  const height = svg.clientHeight || 620;
  const nodes = graphNodes(limit);
  const nodeIds = new Set(nodes.map((node) => node.id));
  const positions = assignPositions(nodes, width, height);
  const selectedLinks = state.links.filter((link) => nodeIds.has(link.source) && nodeIds.has(link.target));

  els.graphStatus.textContent = `${fmt(nodes.length)} nodes / ${fmt(selectedLinks.length)} links`;
  svg.setAttribute("viewBox", `0 0 ${width} ${height}`);

  const edgeMarkup = selectedLinks.map((link) => {
    const source = positions.get(link.source);
    const target = positions.get(link.target);
    const focused = state.selectedId === link.source || state.selectedId === link.target ? " focus" : "";
    const strokeWidth = Math.min(4, 0.7 + Math.log2(link.count + 1));
    return `<line class="edge${focused}" x1="${source.x}" y1="${source.y}" x2="${target.x}" y2="${target.y}" stroke-width="${strokeWidth}"></line>`;
  }).join("");

  const labelEvery = nodes.length <= 100 ? 1 : nodes.length <= 180 ? 2 : 4;
  const nodeMarkup = nodes.map((method, index) => {
    const pos = positions.get(method.id);
    const selected = state.selectedId === method.id ? " selected" : "";
    const changed = method.change ? " changed" : "";
    const label = index % labelEvery === 0 ? `<text class="node-label" x="${pos.x + nodeRadius(method) + 3}" y="${pos.y + 3}">${escapeHtml(method.name.slice(0, 28))}</text>` : "";
    return `
      <g data-id="${method.id}">
        <circle class="node ${method.coverage}${selected}${changed}" cx="${pos.x}" cy="${pos.y}" r="${nodeRadius(method)}">
          <title>${escapeHtml(method.fullName)}</title>
        </circle>
        ${label}
      </g>
    `;
  }).join("");

  svg.innerHTML = `<g>${edgeMarkup}</g><g>${nodeMarkup}</g>`;
  svg.querySelectorAll("g[data-id]").forEach((group) => {
    group.addEventListener("click", () => selectMethod(Number(group.dataset.id)));
  });
}

function selectMethod(id) {
  state.selectedId = id;
  renderDetails();
  renderGraph(Number(els.nodeLimit.value));
}

function relationItems(id, direction) {
  const links = direction === "out" ? state.linksOut.get(id) || [] : state.linksIn.get(id) || [];
  return links.slice(0, 16).map((link) => {
    const otherId = direction === "out" ? link.target : link.source;
    const method = state.methodById.get(otherId);
    if (!method) return "";
    return `<li><button type="button" data-id="${otherId}"><span class="mono">${escapeHtml(methodTitle(method))}</span> <span>(${link.count})</span></button></li>`;
  }).join("");
}

function renderDetails() {
  if (state.selectedId === null) {
    els.details.className = "details-empty";
    els.details.innerHTML = "No function selected.";
    return;
  }

  const method = state.methodById.get(state.selectedId);
  const domainBadges = method.domains.map((id) => {
    const domain = state.domainsById.get(id);
    return `<span class="badge native-owner">${id} ${escapeHtml(domain ? domain.title : "")}</span>`;
  }).join("");
  const systemBadges = method.systems.map((id) => `<span class="badge system-map">${escapeHtml(id)}</span>`).join("");
  const changeBadge = method.change ? `<span class="badge changed">${escapeHtml(method.change)}</span>` : "";

  els.details.className = "details";
  els.details.innerHTML = `
    <h3 class="mono">${escapeHtml(method.name)}</h3>
    <div class="badge-line">
      <span class="badge ${method.coverage}">${coverageLabel(method.coverage)}</span>
      ${changeBadge}
      ${domainBadges}
      ${systemBadges}
    </div>
    <dl>
      <dt>Type</dt><dd class="mono">${escapeHtml(method.type)}</dd>
      <dt>Signature</dt><dd class="mono">${escapeHtml(method.signature)}</dd>
      <dt>Return</dt><dd class="mono">${escapeHtml(method.returnType)}</dd>
      <dt>Params</dt><dd class="mono">${escapeHtml(method.parameters || "-")}</dd>
      <dt>Token</dt><dd class="mono">${escapeHtml(method.token)}</dd>
      <dt>Visibility</dt><dd>${escapeHtml(method.visibility)}</dd>
      <dt>Body size</dt><dd>${fmt(method.bodySize)}</dd>
      <dt>Calls</dt><dd>${fmt(method.callsIn)} incoming / ${fmt(method.callsOut)} outgoing / ${fmt(method.externalCallsOut)} external</dd>
      <dt>Role hint</dt><dd>${escapeHtml(method.roleHint)}</dd>
      <dt>Symbols</dt><dd class="mono">${escapeHtml((method.domainSymbols || []).join(", ") || "-")}</dd>
    </dl>
    <h2>Calls</h2>
    <ul class="relation-list">${relationItems(method.id, "out") || "<li><button disabled>No internal callees</button></li>"}</ul>
    <h2>Callers</h2>
    <ul class="relation-list">${relationItems(method.id, "in") || "<li><button disabled>No internal callers</button></li>"}</ul>
  `;

  els.details.querySelectorAll("button[data-id]").forEach((button) => {
    button.addEventListener("click", () => selectMethod(Number(button.dataset.id)));
  });
}

function wireEvents() {
  const update = () => {
    clearTimeout(state.debounceTimer);
    state.debounceTimer = window.setTimeout(applyFilters, 80);
  };

  for (const element of [els.searchInput, els.domainSelect, els.systemSelect, els.coverageSelect, els.visibilitySelect, els.nodeLimit]) {
    element.addEventListener("input", update);
    element.addEventListener("change", update);
  }

  els.clearSelection.addEventListener("click", () => {
    state.selectedId = null;
    renderDetails();
    renderGraph(Number(els.nodeLimit.value));
  });

  window.addEventListener("resize", () => renderGraph(Number(els.nodeLimit.value)));
}

async function main() {
  initElements();
  try {
    await loadData();
    populateFilters();
    renderOverview();
    renderCharts();
    wireEvents();
    applyFilters();
    renderDetails();
  } catch (error) {
    console.error(error);
    els.buildLine.textContent = `Failed to load generated data: ${error.message}. Serve this directory over HTTP and select one completed data set.`;
    els.graphStatus.textContent = "Load failed";
  }
}

main();
