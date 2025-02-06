import {checkUser} from "./checkUser.js";
import {one, oneByName} from "./repo.js";
import {loadNavBar} from "./nav-bar.js";

const searchParams = new URLSearchParams(window.location.search);
const type = searchParams.get("type");
const name = searchParams.get("name");

let id = searchParams.get("id");
let schema;
let editor ;

$(document).ready(function() {
    loadNavBar();
    $("#entityActions").prop('hidden', type !=="entity");
    $("#menuActions").prop('hidden', type !=="menu");
    
    
    $('#saveMenu').on('click', ()=> submit(save));
    $('#saveEntity').on('click', ()=> submit(save));
    $('#define').on('click',getDefine);
    $('#saveDefine').on('click', ()=> submit(saveDefine));
    $('#editContent').on('click', function() {
        const val = editor.getValue();
        window.open(`../admin/entities/${val.name}`,'_blank');
    });
    $('#showAdvancedActions').change(function() {
        $("#advancedEntityActions").prop('hidden',!$(this).is(':checked'));
    });
    
    checkUser(loadEditor);
});

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
        $('#errorPanel').text(error).show();
    }   
}

function loadEditor() {
    let editor = new JSONEditor($('#editor_holder')[0], {
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
        if (id || name) {
            
            $.LoadingOverlay("show");
            const {data:schemaData, error} = id ? await one(id):await oneByName(name, type);
            $.LoadingOverlay("hide");
            
            if (schemaData){
                $('title').text(`${schemaData.name} - ${type} setting - FormCMS Schema Builder`);
                schema = schemaData;
                editor.setValue(schemaData.settings[type]);
            }else {
                $('#errorPanel').text(error).show();
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
async function submit(callback) {
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
        $('#errorPanel').text('').hide();
        window.location.href = `edit.html?type=${type}&id=${data.id}`;
    } else {
        $('#errorPanel').text(error).show();
    }
}