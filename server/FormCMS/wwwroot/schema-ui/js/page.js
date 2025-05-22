import {loadEditor} from "../grapes-components/grapes.js";
import {checkUser} from "./checkUser.js";
import {loadNavBar} from "./nav-bar.js";
import {single, save} from "./repo.js";
import {queryKeys, schemaTypes} from "./types.js";
import {getParams} from "./searchParamUtil.js";

let schema;
let editor;

const [navBox, headerBox,inputsBox,grapesBox, errorBox] = 
    ['#nav-box','#header-box','#inputs-box','#grapes-box','#error-box'];

$(document).ready(function() {
    // as small scope as possible
    const [id,refId] = getParams([queryKeys.id,queryKeys.refId]);
    loadNavBar(navBox);
    addActionButtons();
    addInputs();
    checkUser(()=>init(id,refId));
});

async function init(id, refId) {
    if (!id && !refId) {
        editor = loadEditor(grapesBox);
        return
    }
    
    const schemaData = await loadSchema(id || refId);
    if (schemaData) {
        const pageData = schemaData.settings[schemaTypes.page];
        if (id) {
            schema = schemaData;
        } else {
            schema = {type: schemaTypes.page, settings: {[schemaTypes.page]: pageData}};
        }
        restoreFormData(pageData);
        editor = loadEditor(grapesBox, JSON.parse(pageData.components), JSON.parse(pageData.styles));
    }
}

async function handleSave() {

    const payload = {
        html: editor.getHtml(),
        css: editor.getCss(),
        components:JSON.stringify(editor.getComponents()),
        styles: JSON.stringify(editor.getStyle()),
        type: schemaTypes.page,
    };
    if (!attachFormData(payload)){
        return false;
    }

    schema = schema ||{type:schemaTypes.page};
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

        schema = data;
        history.pushState(null, "", `page.html?${queryKeys.id}=${data.id}`);
        $(errorBox).text('').hide();
    } else {
        $(errorBox).text(error).show();
    }
}

async function loadSchema(idVal){
    if (!idVal){
        return null;
    }
    $.LoadingOverlay("show");
    const {data: schemaData, error} = await single(idVal);
    $.LoadingOverlay("hide");

    if (error) {
        $(errorBox).text(error).show();
        return null;
    }
    return schemaData;
}

function addInputs(){
    $(inputsBox).html(`
          <div class="row">
             <div class="col-md-4 mb-3">
                 <label for="name" class="form-label">Page Name</label>
                 <input id="name" type="text" class="form-control" required>
             </div>
             
             <div class="col-md-4 mb-3">
                 <label for="title" class="form-label">Page Title</label>
                 <input id="title" type="text" class="form-control" required>
             </div>

             <div class="col-md-4 mb-3">
                 <label for="query" class="form-label">Query</label>
                 <input id="query" type="text" class="form-control">
             </div>
         </div>
    `);
}
const controls = ['name','title', 'query'];
function restoreFormData(payload){
    for (const ctl of controls){
        $(`#${ctl}`).val(payload[ctl]);
    }
}

function attachFormData(payload){
    for (const ctlName of controls){
        const ctl = $(`#${ctlName}`);
        if (ctl.prop('required') && !ctl.val()){
            $(errorBox).text(`${ctlName} can not be empty`).show();
            return false;
        }
        payload[ctlName] = ctl.val();
    }
    return true;
}

function addActionButtons() {
    $(headerBox).html(`
     <button id='savePage' class="btn btn-primary">Save Page</button>
     <button id='visitPage' class="btn btn-primary">View Page</button>
    `);

    $('#visitPage').on('click', function () {
        const name = $(`#name`).val();
        if (name){
            window.open(`/${name}/?${queryKeys.sandbox}=1`, '_blank'); // Opens in a new tab
        }
    });
    
    $('#savePage').on('click', handleSave);
}