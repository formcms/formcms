import {checkUser} from "./checkUser.js";
import {renderSchemaTable} from "./listUtil.js";
import {list} from "./repo.js";
import {loadNavBar} from "./nav-bar.js";

const searchParams = new URLSearchParams(window.location.search);
const type = searchParams.get("type");

$(document).ready(function() {
    loadNavBar();
    $('#addEntity').prop('hidden', type !=='entity');
    $('#addPage').prop('hidden', type !=='page');
    $('#addQuery').prop('hidden', type !=='query');
    
    if (type){
        $('title').text(`${type} list - Fluent CMS Schema Builder`);
    }
    checkUser(async ()=>{
        await renderSchemaTable(
            "datatable-container", 
            ()=>list(type), 
            {showDelete:true, showViewHistory:true}
        );
    });
});