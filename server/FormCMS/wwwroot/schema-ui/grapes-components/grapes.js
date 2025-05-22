import {addCustomTypes} from "./custom-types.js"
import {addCustomBlocks} from "./custom-blocks.js"
import {extendDomComponents} from "./domConponents.js";

//copy from grapes.js demo
export function loadEditor(container,  components, styles) {
    let editor = grapesjs.init({
        storageManager: false,
        container: container,
        plugins: [
            'gjs-blocks-basic',
            'grapesjs-custom-code',
            'grapesjs-preset-webpage'
       ],
        pluginsOpts:{
            'gjs-blocks-basic': { 
                flexGrid: true,
                blocks: ['column1', 'column2', 'column3', 'column3-7' ,'text', 'link'/*, 'image', 'video', 'map'*/]
            },
            'grapesjs-preset-webpage': {
                blocks:[],
                modalImportTitle: 'Import Template',
                modalImportLabel: '<div style="margin-bottom: 10px; font-size: 13px;">Paste here your HTML/CSS and click Import</div>',
            },
        },
        canvas: {
            scripts: [
                'https://cdn.tailwindcss.com'
            ],
            styles: [
                'https://cdnjs.cloudflare.com/ajax/libs/tailwindcss/2.0.2/tailwind.min.css',
                'https://cdn.jsdelivr.net/npm/daisyui@latest/dist/full.min.css',
            ],
        },
        assetManager: {
            assets: findUniqueImageUrls(components),
            uploadName: 'files'

            // options
        }
    });

    var pn = editor.Panels;

    // Add and beautify tooltips
    [['sw-visibility', 'Show Borders'], ['preview', 'Preview'], ['fullscreen', 'Fullscreen'],
        ['export-template', 'Export'], ['undo', 'Undo'], ['redo', 'Redo'],
        ['gjs-open-import-webpage', 'Import'], ['canvas-clear', 'Clear canvas']]
        .forEach(function(item) {
            pn.getButton('options', item[0]).set('attributes', {title: item[1], 'data-tooltip-pos': 'bottom'});
        });
    [['open-sm', 'Style Manager'], ['open-layers', 'Layers'], ['open-blocks', 'Blocks']]
        .forEach(function(item) {
            pn.getButton('views', item[0]).set('attributes', {title: item[1], 'data-tooltip-pos': 'bottom'});
        });
    const titles = document.querySelectorAll('*[title]');

    for (let i = 0; i < titles.length; i++) {
        const el = titles[i];
        let title = el.getAttribute('title');
        title = title ? title.trim(): '';
        if(!title)
            break;
        el.setAttribute('data-tooltip', title);
        el.setAttribute('title', '');
    }
    // Do stuff on load
    editor.on('load', function() {
        // Show borders by default
        pn.getButton('options', 'sw-visibility').set({
            command: 'core:component-outline',
            'active': true,
        });
        editor.setComponents(components);
        editor.setStyle(styles);
    });
    
    addCustomTypes(editor);
    addCustomBlocks(editor);
    extendDomComponents(editor);
    return editor;
}

function findUniqueImageUrls(components) {
    const imageUrls = new Set(['{{image.url}}']);

    function iterateComponents(compArray) {
        if (!Array.isArray(compArray)) return;

        compArray.forEach(component => {
            // Check if the component is of type "image" and has a src attribute
            if (component.type === "image" && component.attributes && component.attributes.src) {
                imageUrls.add(component.attributes.src);
            }

            // Recursively iterate through nested components
            if (component.components) {
                iterateComponents(component.components);
            }
        });
    }

    iterateComponents(components);
    return Array.from(imageUrls);
}