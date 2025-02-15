import {checkUser} from "./checkUser.js";
import {single, singleByName, save, saveDefine, define} from "./repo.js";
import {loadNavBar} from "./nav-bar.js";
import {queryKeys} from "./types.js";

let schema;
let editor ;
const [navBox, headerBox,editorBox, errorBox] = ['#nav-box','#header-box','#editor-box','#error-box'];

$(document).ready(function() {
    const searchParams = new URLSearchParams(window.location.search);
    const type = searchParams.get(queryKeys.type);
    const name = searchParams.get(queryKeys.name);
    const refId = searchParams.get(queryKeys.refId);
    let id = searchParams.get(queryKeys.id);
    
    
    loadNavBar(navBox);
    if (type === "entity"){
        addEntityActionsButtons();
    }else if(type === "menu"){
        addMenuActionsButtons();
    }
    checkUser(()=>loadEditor(id,name,type,refId));
});

function addMenuActionsButtons() {
    $(headerBox).html(`
    <button id='saveMenu' class="btn btn-primary">Save Menu</button>
    `);
    $('#saveMenu').on('click', ()=> submit('menu',save));
}

function addEntityActionsButtons() {
    $(headerBox).html(`
    <button id='saveDefine' class="btn btn-primary">Save Schema</button>
    <button id='editContent' class="btn btn-primary">Edit Content</button>
    <span >
        <input type="checkbox" class="form-check-input" id="showAdvancedActions">
        <label for="showAdvancedActions">Advanced Actions</label>
    </span>
    <span id="advancedEntityActions" hidden>
        <button id='define' class="btn btn-primary">Get Columns Definition from Database</button>
        <button id='saveEntity' class="btn btn-primary">Save Schema Not Update Database</button>
    </span>`
    );

    $('#saveDefine').on('click', ()=> submit('entity',saveDefine));
    $('#editContent').on('click', function() {
        const val = editor.getValue();
        window.open(`../admin/entities/${val.name}`,'_blank');
    });
    $('#showAdvancedActions').change(function() {
        $("#advancedEntityActions").prop('hidden',!$(this).is(':checked'));
    });
    $('#define').on('click',getDefine);
    $('#saveEntity').on('click', ()=> submit('entity',save));

}

async function getDefine(){
    const oldValue = editor.getValue();
    const tableName = oldValue.tableName;

    $.LoadingOverlay("show");
    const {data, error} = await define(tableName);
    $.LoadingOverlay("hide");

    if (data){
        oldValue.attributes = data.attributes;
        editor.setValue(oldValue);
    }else {
        $(errorBox).text(error).show();
    }   
}

function loadEditor(id, name, type, refId) {
    editor = new JSONEditor($(editorBox)[0], {
        ajax: true,
        schema: {
            "$ref": `json/${type}.json`,
        },
        compact: true,
        disable_collapse: true,
        disable_array_delete_last_row: true,
        disable_array_delete_all_rows: true,
        disable_properties: true,
        disable_edit_json: false,
        collapsed: true,
        object_layout: "grid",
        show_errors: 'always'
    });

    editor.on('ready',async function() {
        if (id || name || refId) {
            
            $.LoadingOverlay("show");
            const {data:schemaData, error} = id 
                ?await single(id)
                : refId ? 
                    await single(refId):
                    await singleByName(name, type);
            
            $.LoadingOverlay("hide");
            
            if (schemaData){
                $('title').text(`${schemaData.name} - ${type} setting - FormCMS Schema Builder`);
                if (refId){
                    //create a new schema
                    schema = {type,settings:{[type]: schemaData.settings[type]}};
                }else {
                    schema = schemaData;
                    
                }
                editor.setValue(schemaData.settings[type]);
            }else {
                $(errorBox).text(error).show();
            }
        } else {
            editor.setValue(null);
        }
    });

    editor.on('change', function() {
        let errors = editor.validate();
        let indicator = $('#valid_indicator');
        if (errors.length) {
            indicator.css('color', 'red').text("not valid");
        } else {
            indicator.css('color', 'green').text("valid");
        }
    });
    return editor;
}
async function submit(type, callback) {
    const errors = editor.validate();
    if (errors.length) {
        return;
    }

    schema = schema ||{type}
    schema.settings = {[type]:editor.getValue()};
    
    $.LoadingOverlay("show");
    const {data, error} = await callback(schema);
    $.LoadingOverlay("hide");
    
    if (data) {
        $.toast({
            heading: 'Success',
            text: 'submit succeed!',
            showHideTransition: 'slide',
            icon: 'success'
        })
        $(errorBox).text('').hide();
        schema = data;
        history.pushState(null, "", `edit.html?${queryKeys.type}=${type}&${queryKeys.id}=${data.id}`);
    } else {
        $(errorBox).text(error).show();
    }
}