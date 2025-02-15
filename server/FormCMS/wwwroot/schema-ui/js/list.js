import {checkUser} from "./checkUser.js";
import {renderSchemaTable} from "./listUtil.js";
import {list} from "./repo.js";
import {loadNavBar} from "./nav-bar.js";
import {queryKeys, schemaTypes} from "./types.js";

const [navBox, headerBox,tableBox, errorBox] = ['#nav-box','#header-box','#table-box','#error-box'];

$(document).ready(function() {
    const searchParams = new URLSearchParams(window.location.search);
    const type = searchParams.get(queryKeys.type);
    loadNavBar(navBox);
    loadHeaderBar(type);
    checkUser(async ()=>{
        await renderSchemaTable(
            tableBox,
            errorBox,
            ()=>list(type), 
            {showDelete:true, showViewHistory:true}
        );
    });
});

function loadHeaderBar(type) {
    let html ;
    if (type === schemaTypes.entity) {
        html = `<a id="addEntity" class="btn btn-primary" href="./edit.html?type=entity">Add Entity</a>`
    }else if (type === schemaTypes.page) {
        html = `<a id="addPage" class="btn btn-primary" href="./page.html">Add Page</a>`
    }else if (type === schemaTypes.query) {
        html = `<a id="addQuery" class="btn btn-primary" href="/api/schemas/graphql">Add Query</a>`
    }
    $(headerBox).html(html);
}