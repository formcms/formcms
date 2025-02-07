import {loadEditor} from "../grapes-components/grapes.js";
import {checkUser} from "./checkUser.js";
import {loadNavBar} from "./nav-bar.js";
import {one, save} from "./repo.js";

let id = new URLSearchParams(window.location.search).get("id");
let schema;
let editor;

$(document).ready(function() {
    loadNavBar();

    $('#visitPage').on('click', function () {
        const name = $(`#name`).val();
        if (name){
            window.open(`/${name}/?sandbox=1`, '_blank'); // Opens in a new tab
        }
    });
    
    $('#savePage').on('click', handleSave);
    
    checkUser(()=>{
        editor = loadEditor("#gjs", loadData);
    });
});

async function handleSave() {

    const payload = {
        html: editor.getHtml(),
        css: editor.getCss(),
        components:JSON.stringify(editor.getComponents()),
        styles: JSON.stringify(editor.getStyle()),
        type: 'page',
    };
    if (!collectFormData(payload)){
        return false;
    }
    
    schema = schema ||{type:'page'};
    schema.settings = {page:payload};

    $.LoadingOverlay("show");
    const {data, error} = await save(schema);
    $.LoadingOverlay("hide");

    if (data) {
        $.toast({
            heading: 'Success',
            text: 'submit succeed!',
            showHideTransition: 'slide',
            icon: 'success'
        })
        window.location.href = `page.html?id=${data.id}`;
        $('#errorPanel').text('').hide();
    } else {
        $('#errorPanel').text(error).show();
    }
}
async function loadData(editor) {
    if (!id) {
        return;
    }
    
    $.LoadingOverlay("show");
    const {data: schemaData, error} = await one(id);
    $.LoadingOverlay("hide");

    if (error) {
        $('#errorPanel').text(error).show();
        return;
    }
    schema = schemaData;

    $('title').text(`🏠${schemaData.name} - page setting - Fluent CMS Schema Builder`);
    id = schemaData.id;
    const pageData = schemaData.settings['page'];
    restoreFormData(pageData);
    editor.setComponents(JSON.parse(pageData.components));
    editor.setStyle(JSON.parse(pageData.styles));
}


const controls = ['name','title', 'query', 'queryString'];
function restoreFormData(payload){
    for (const ctl of controls){
       $(`#${ctl}`).val(payload[ctl]);
    }
}

function collectFormData(payload){
    for (const ctlName of controls){
        const ctl = $(`#${ctlName}`);
        if (ctl.prop('required') && !ctl.val()){
            $('#errorPanel').text(`${ctlName} can not be empty`).show();
            return false;
        }
        payload[ctlName] = ctl.val();
    }
    return true;
}