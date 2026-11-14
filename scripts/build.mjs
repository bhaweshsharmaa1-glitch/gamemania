import { cp, mkdir, rm } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const output = path.join(root, "www");
const webFiles = [
  "index.html",
  "home.css",
  "manifest.webmanifest",
  "privacy-policy.html",
  "assets/icon.svg",
  "shared",
  "connect-four",
  "rock-paper-scissors",
  "memory-match",
  "2048",
  "whack-a-mole"
];

await rm(output, { recursive: true, force: true });
await mkdir(output, { recursive: true });
for (const file of webFiles) {
  const destination = path.join(output, file);
  await mkdir(path.dirname(destination), { recursive: true });
  await cp(path.join(root, file), destination, { recursive: true });
}
console.log(`Built the standalone web edition at ${path.relative(root, output)}.`);
