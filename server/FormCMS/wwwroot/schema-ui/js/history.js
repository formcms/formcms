import {checkUser} from "./checkUser.js";
import {getHistory} from "./repo.js";
import {renderSchemaTable} from "./listUtil.js";
import {loadNavBar} from "./nav-bar.js";
import {queryKeys} from "./types.js";
import {getParams} from "./searchParamUtil.js";

const [navBox, headerBox, tableBox, errorBox] 
    = ['#nav-box','#header-box','#table-box','#error-box'];

$(document).ready( function() {
    const [schemaId, type] = getParams([queryKeys.schemaId, queryKeys.type]);
    loadNavBar(navBox);
    AddActionButton(type);
    
    checkUser(async ()=>        
        renderSchemaTable(
            tableBox,
            errorBox,
            ()=>getHistory(schemaId),
            {showDiff:true, showDelete:false, showViewHistory: false}
        )
    );
});

function AddActionButton(type) {
    $(headerBox).html(`<a href="list.html?${queryKeys.type}=${type}"  class="btn btn-secondary btn-sm badge">Back</a>`);
}
