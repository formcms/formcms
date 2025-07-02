import {addCustomTypes} from "./custom-types.js"
import {addCustomBlocks} from "./custom-blocks.js"
import {extendDomComponents} from "./domConponents.js";


const darkStyles = `[data-theme="dark"] body {
            background-color: #1a202c;
            color: #e2e8f0;
        }
        [data-theme="dark"] .text-gray-900 {
            color: #e2e8f0;
        }
        [data-theme="dark"] .text-gray-800 {
            color: #cbd5e1;
        }

        [data-theme="dark"] .text-gray-700 {
            color: #94a3b8;
        }

        [data-theme="dark"] .text-gray-600 {
            color: #a0aec0;
        }

        [data-theme="dark"] .text-gray-500 {
            color: #cbd5e1;
        }

        [data-theme="dark"] .bg-white {
            background-color: #2d3748;
        }

        [data-theme="dark"] .bg-gray-100 {
            background-color: #4a5568;
        }

        [data-theme="dark"] .border-gray-200 {
            border-color: #4a5568;
        }

        [data-theme="dark"] .hover\\:text-gray-900:hover {
            color: #e2e8f0;
        }

        [data-theme="dark"] .hover\\:text-blue-600:hover {
            color: #63b3ed;
        }

        [data-theme="dark"] .hover\\:text-blue-500:hover {
            color: #90cdf4;
        }

        [data-theme="dark"] .hover\\:text-blue-400:hover {
            color: #63b3ed;
        }

        [data-theme="dark"] .bg-indigo-400 {
            background-color: #5a67d8;
        }

        [data-theme="dark"] .hover\\:bg-indigo-500:hover {
            background-color: #667eea;
        }

        [data-theme="dark"] .text-red-500 {
            color: #fc8181;
        }

        [data-theme="dark"] .hover\\:text-indigo-600:hover {
            color: #7f9cf5;
        }

        [data-theme="dark"] .hover\\:text-indigo-700:hover {
            color: #667eea;
        }

        [data-theme="dark"] .bg-gradient-to-r.from-violet-300.to-indigo-300 {
            background: linear-gradient(to right, #9f7aea, #5a67d8);
        }
        `;
//copy from grapes.js demo
export function loadEditor(container,  components, styles) {
    // styles += darkStyles;
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
                '/_content/FormCMS/static-assets/css/dark.css'
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
