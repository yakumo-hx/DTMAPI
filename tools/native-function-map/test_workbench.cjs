"use strict";
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const { webcrypto, createHash } = require("node:crypto");

const root = path.join(__dirname, "workbench");
const script = fs.readFileSync(path.join(root, "app.js"), "utf8").replace(/\nmain\(\);\s*$/, "");
let payloads = Object.fromEntries(["summary", "methods", "links"].map((name) => [name, fs.readFileSync(path.join(root, `data/${name}.json`))]));
const context = vm.createContext({ console, TextDecoder, crypto: webcrypto, fetch: async (url) => {
  const name = url.match(/data\/(.+)\.json/)[1];
  const bytes = payloads[name];
  return { ok: !!bytes, status: bytes ? 200 : 404, arrayBuffer: async () => bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) };
} });
vm.runInContext(script, context);

async function main() {
  await vm.runInContext("loadData()", context);
  vm.runInContext(`for (const key of ["buildLine", "methodCount", "edgeCount", "ownerCoverage", "mapCoverage", "unmappedCoverage"]) els[key] = { textContent: "" }; renderOverview();`, context);
  const line = vm.runInContext("els.buildLine.textContent", context);
  assert.ok(line.includes("23465763"));
  assert.ok(line.includes("38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228"));
  assert.ok(line.includes("current game/API support is not implied"));
  assert.equal(vm.runInContext("state.methods.length", context), 42925);
  assert.equal(vm.runInContext("state.links.length", context), 74488);

  payloads = { methods: Buffer.from(JSON.stringify([{ id: 1 }])), links: Buffer.from("[]") };
  const summary = { meta: { methodCount: 1, internalEdgeCount: 0, generation: { status: "complete", outputs: ["methods", "links"].map((name) => ({ path: `${name}.json`, bytes: payloads[name].length, sha256: createHash("sha256").update(payloads[name]).digest("hex") })) } }, domains: [] };
  payloads.summary = Buffer.from(JSON.stringify(summary));
  await vm.runInContext("loadData()", context);
  payloads.methods = Buffer.from(JSON.stringify([{ id: 2 }]));
  await assert.rejects(vm.runInContext("loadData()", context), /Mixed or damaged methods/);
  delete summary.meta.generation;
  summary.meta.methodCount = 2;
  payloads.summary = Buffer.from(JSON.stringify(summary));
  await assert.rejects(vm.runInContext("loadData()", context), /Mixed or incomplete/);
  console.log("NATIVE FUNCTION MAP WORKBENCH: OK (retained actual baseline, graph identity, generated output hashes, mixed data rejection)");
}
main().catch((error) => { console.error(error); process.exitCode = 1; });
