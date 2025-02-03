import {useTreeData} from "../services/entity";
import { XEntity } from "../types/xEntity";

export function useTree(entity:XEntity) {
    const {data: options} = useTreeData(entity.name);
    function setTreeProperties(data: any[]) {
        data.forEach(item => {
            item['label'] = item[entity.primaryKey] + " " + item[entity.labelAttributeName]
            item['key'] = item[entity.primaryKey]
            setTreeProperties(item.children ?? [])
        })
    }
    setTreeProperties(options??[]);
    return options;
}