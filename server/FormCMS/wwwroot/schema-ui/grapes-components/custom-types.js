export const customTypes = {
    ['data-list']: [
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
    ['top-list']:[
        {name:"offset", label:'offset'},
        {name:"entity", label:'entity'},
        {name:"limit", label:'limit'},
    ],
    ['activity-bar']:[
        {name:"data-entity", label:'Entity Name'},
        {name:"data-record-id", label:'Record Id'},
    ]
};