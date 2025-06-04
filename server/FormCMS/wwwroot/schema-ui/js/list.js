import { checkUser } from "./util/checkUser.js";
import { renderSchemaTable } from "./util/listUtil.js";
import { list } from "./services/services.js";
import { loadNavBar } from "./components/navbar.js";
import { queryKeys, schemaTypes } from "./models/types.js";
import { getParams } from "./util/searchParamUtil.js";

checkUser(async () => {
    // Get element references using querySelector
    const navBox = document.querySelector('#nav-box');
    const headerBox = document.querySelector('#header-box');
    const tableBox = document.querySelector('#table-box');
    const errorBox = document.querySelector('#error-box');

    const [type] = getParams([queryKeys.type]);

    loadNavBar(navBox);
    loadHeaderBar(type);
    await renderSchemaTable(
        tableBox,
        errorBox,
        () => list(type),
        { showDelete: true, showViewHistory: true }
    );
    function loadHeaderBar(type) {
        let html = '';
        if (type === schemaTypes.entity) {
            html = `<a id="addEntity" class="btn btn-primary" href="./edit.html?type=entity">Add Entity</a>`;
        } else if (type === schemaTypes.page) {
            html = `<a id="addPage" class="btn btn-primary" href="./page.html">Add Page</a>`;
        } else if (type === schemaTypes.query) {
            html = `<a id="addQuery" class="btn btn-primary" href="/api/schemas/graphql">Add Query</a>`;
        }

        if (headerBox) {
            headerBox.innerHTML = html;
        }
    }
});


