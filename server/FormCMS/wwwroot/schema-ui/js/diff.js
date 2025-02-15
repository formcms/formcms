import {loadNavBar} from "./nav-bar.js";
import {single} from "./repo.js";
import {getParams} from "./searchParamUtil.js";
import {queryKeys} from "./types.js";

const [navBox,headerBox, diffBox] = ["#nav-box","#header-box","#diff-box"];
$(document).ready(function() {
    const [oldId,newId,schemaId, type] = getParams([queryKeys.oldId,queryKeys.newId, queryKeys.schemaId, queryKeys.type]);
    addActionButtons(schemaId, type); 
    loadNavBar(navBox);
    initializeEditor(oldId,newId);
});

function addActionButtons(schemaId,type){
    $(headerBox).html(
    `<a href="history.html?${queryKeys.schemaId}=${schemaId}&${queryKeys.type}=${type}"  
            class="btn btn-secondary btn-sm badge">Back</a>`
    );
    
}

function initializeEditor(oldId,newId) {
    require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs' } });
    require(["vs/editor/editor.main"], async function () {
        const diffEditor = monaco.editor.createDiffEditor($(diffBox)[0], {
            theme: "vs-dark",
            readOnly: false,
            enableSplitViewResizing: true,
        });
        
        $.LoadingOverlay("show");
        let [{data: oldSchema, error: err1},{data: newSchema, error:err2}] = await Promise.all([ single(oldId), single(newId)]);
        $.LoadingOverlay("hide");
        
        if (err1|| err2){
            const error = err1.error + err2.error;
            $('#errorPanel').text(error).show(); 
            return;
        }
        const type = oldSchema.type;

        oldSchema = oldSchema.settings[type];
        newSchema = newSchema.settings[type];
        
        let lan; 
        if(type === 'query') {
            lan = "graphql";
            oldSchema = oldSchema.source;
            newSchema = newSchema.source;
        }else if (type === 'page') {
            lan = "json";
            oldSchema = JSON.stringify(JSON.parse(oldSchema.components),null, 2);
            newSchema = JSON.stringify(JSON.parse(newSchema.components),null, 2);
        }else{
            lan = "json";
            oldSchema = JSON.stringify(oldSchema,null,2);
            newSchema = JSON.stringify(newSchema,null,2);
        }
        
        
        diffEditor.setModel({
            original: monaco.editor.createModel(oldSchema, lan),
            modified: monaco.editor.createModel(newSchema, lan),
        });
    });
}