export function extendDomComponents(editor){
    extendImage(editor);
}

function extendImage(editor) {
    editor.DomComponents.addType('image', {
        extend: 'image',
        model: {
            defaults: {
                // Merge existing traits with the new src trait
                traits: [
                    // Keep default traits (alt, title, etc.)
                    ...editor.DomComponents.getType('image').model.prototype.defaults.traits,
                    // Add custom src trait
                    {
                        type: 'text', // or 'url' if you want URL-specific behavior
                        name: 'src',
                        label: 'Source',
                        placeholder: 'https://example.com/image.jpg',
                        changeProp: true, // Bind to component property instead of attribute
                    },
                ],
                // Ensure src is set in attributes if needed
                attributes: {
                    src: 'https://placehold.it/350x250', // Default placeholder
                },
            },
            // Initialize the component and listen for src changes
            init() {
                this.on('change:src', this.handleSrcChange);
            },
            // Handle src trait changes
            handleSrcChange() {
                const src = this.get('src');
                // Update the component's src attribute
                this.addAttributes({src});
            },
        },
    });
}