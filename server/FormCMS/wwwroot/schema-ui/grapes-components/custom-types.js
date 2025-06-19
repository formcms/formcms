

export function addCustomTypes(editor){
    for(const {name,traits,openTm} of customTypes){
        editor.Components.addType(name, {
            model: {
                defaults: {
                    traits,
                    attributes: { id: `${name}-${Date.now()}-${Math.floor(Math.random() * 1000)}` },
                },
            },
            view:{
                openSettings: function ( e ) {
                    e.preventDefault();
                    editor.select(this.model);
                    
                    if(openTm) {
                        editor.Panels.getButton('views', 'open-tm').set('active', 1);
                    }
                },
                onActive() {
                    this.el.contentEditable = true;
                },
                events:{
                    dblclick: 'onActive',
                    click: `openSettings`,
                    selected: `openSettings`
                }
            }
        });
    }
}
export const customTypes = [
    {
        //just need generate unique id
        name:'foreach',
        openTm:false,
        traits:[]
    },
    {
        name: 'data-list',
        traits:[
            {name:"field", label:'field'},
            {name:"query", label:'query'},
            {name:"qs", label:'qs'},
            {name:"offset", label:'offset'},
            {name:"limit", label:'limit'},
            {
                type: 'select',
                label: 'Pagination',
                name: 'pagination',
                options: [
                    { value: 'None', name: 'None' },
                    { value: 'Button', name: 'Button' },
                    { value: 'InfiniteScroll', name: 'Infinite Scroll' },
                ],
            }       
        ],
        openTm: true
    },
    {
        name: 'comment-form',
        openTm:true,
        traits: [
            {name: "data-entity", label: 'Entity Name'},
        ]
    },
    /*standalone activity bar, load counts and status, 
    contrary to activity-bar in 'top list' */
    {
        name: 'activity-bar',
        openTm:true,
        traits: [
            {name:"data-entity", label:'Entity Name'},
            {name:"data-record-id", label:'Record Id'},
            {
                name:"data-fetch-count", 
                label:'Fetch Count',
                type: 'select',
                options: [
                    { value: 'yes', name: 'Yes' },
                    { value: 'no', name: 'No' },
                ]
            }
        ],
    }
]