import {checkUser} from "./checkUser.js";
import {getHistory} from "./repo.js";
import {renderSchemaTable} from "./listUtil.js";
import {loadNavBar} from "./nav-bar.js";
import {queryKeys} from "./types.js";

const [navBox, tableBox, errorBox] = ['#nav-box','#table-box','#error-box'];
$(document).ready( function() {
    const searchParams = new URLSearchParams(window.location.search);
    const schemaId = searchParams.get(queryKeys.schemaId);
    loadNavBar(navBox);
    checkUser(async ()=>        
        renderSchemaTable(
            tableBox,
            errorBox,
            ()=>getHistory(schemaId),
            {showDiff:true, showDelete:false, showViewHistory: false}
        )
    );
});
