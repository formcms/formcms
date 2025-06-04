import { checkUser } from "./util/checkUser.js";
import { renderSchemaTable } from "./util/listUtil.js";
import { getParams } from "./util/searchParamUtil.js";

import { getHistory } from "./services/services.js";
import { loadNavBar } from "./components/navbar.js";
import { queryKeys } from "./models/types.js";

const [navBox, headerBox, tableBox, errorBox] = [
    document.querySelector("#nav-box"),
    document.querySelector("#header-box"),
    document.querySelector("#table-box"),
    document.querySelector("#error-box")
];

const [schemaId, type] = getParams([queryKeys.schemaId, queryKeys.type]);

checkUser(async () => {
    loadNavBar(navBox);
    addActionButton(type);
    await renderSchemaTable(
        tableBox,
        errorBox,
        () => getHistory(schemaId),
        { showDiff: true, showDelete: false, showViewHistory: false }
    );

    function addActionButton(type) {
        headerBox.innerHTML = `
      <a href="list.html?${queryKeys.type}=${type}" class="btn btn-secondary btn-sm badge">Back</a>
    `;
    }
});