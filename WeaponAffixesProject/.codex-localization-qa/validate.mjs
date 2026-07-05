import fs from "node:fs/promises";
import { Workbook } from "@oai/artifact-tool";

const source =
  "C:/Users/thoma/Documents/Github/RAM---Random-Affixes-Mod/Mod Magazine Series/Config/Localization.csv";
const csv = await fs.readFile(source, "utf8");
const workbook = await Workbook.fromCSV(csv, { sheetName: "Localization" });
const inspection = await workbook.inspect({
  kind: "region",
  sheetId: "Localization",
  range: "A1:U8",
  maxChars: 5000,
  tableMaxRows: 8,
  tableMaxCols: 21,
});
console.log(inspection.ndjson);
const preview = await workbook.render({
  sheetName: "Localization",
  range: "A1:U8",
  scale: 1,
  format: "png",
});
await fs.writeFile(
  "C:/Users/thoma/Documents/Github/RAM---Random-Affixes-Mod/WeaponAffixesProject/.codex-localization-qa/preview.png",
  new Uint8Array(await preview.arrayBuffer()),
);
