import {checkUser} from "./checkUser.js";
import {getHistory} from "./repo.js";
import {renderSchemaTable} from "./listUtil.js";
import {loadNavBar} from "./nav-bar.js";

const searchParams = new URLSearchParams(window.location.search);
const schemaId = searchParams.get("schemaId");
$(document).ready( function() {
    loadNavBar();
    checkUser(async ()=>        
        renderSchemaTable("datatable-container", 
            ()=>getHistory(schemaId),
            {showDelete:false, showViewHistory: false}
        )
    );
});
