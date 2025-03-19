import {LookupContainer} from "./LookupContainer";
import {TreeSelectContainer} from "./TreeSelectContainer";

import {TextInput} from "../../components/inputs/TextInput";
import {TextAreaInput} from "../../components/inputs/TextAreaInput";
import {EditorInput} from "../../components/inputs/EditorInput";
import {NumberInput} from "../../components/inputs/NumberInput";
import {DatetimeInput} from "../../components/inputs/DatetimeInput";
import {FileInput} from "../../components/inputs/FileInput";
import {GalleryInput} from "../../components/inputs/GalleryInput";
import {DropDownInput} from "../../components/inputs/DropDownInput";
import {MultiSelectInput} from "../../components/inputs/MultiSelectInput";
import {XAttr} from "../types/xEntity";
import {AssetSelector} from "./AssetSelector";
import {DictionaryInput} from "../../components/inputs/DictionaryInput";
import {AssetMetadataEditor} from "./AssetMetaDataEditor";
import { LocalDatetimeInput } from "../../components/inputs/LocalDatetimeInput";

export function createInput(props: {
    column: XAttr,
    data: any,
    id: any,
    control: any,
    register: any,
    uploadUrl: string,
    getFullAssetsURL: (arg: string) => string
},  mdCols:'col-12' | 'col-4' | 'col-6' | 'col-3') {
    const {field, displayType, options} = props.column
    
    const mdClass= `field col-12 md:${mdCols}`
    
    switch (displayType) {
        case 'dictionary':
            return <DictionaryInput className={'field col-12'} 
                                    {...props} 
                                    key={field}/>
        case 'editor':
            return <EditorInput className={'field col-12'}
                                key={field}
                                {...props}/>

        case 'text':
            return <TextInput className={mdClass} 
                              key={field} 
                              {...props}/>
        case 'textarea':
            return <TextAreaInput className={mdClass} 
                                  key={field} 
                                  {...props}/>
       case 'number':
            return <NumberInput className={mdClass} 
                                key={field} 
                                {...props}/>
        case 'localDatetime':
            return <LocalDatetimeInput className={mdClass} 
                                  inline={false} 
                                  key={field} 
                                  {...props}/>
        case 'datetime':
            return <DatetimeInput className={mdClass}
                                  showTime={true}
                                  inline={false}
                                  key={field}
                                  {...props}/>
        case 'date':
            return <DatetimeInput className={mdClass}
                                  inline={false}
                                  showTime={false}
                              key={field} 
                              {...props}/>
        case 'image':
            return <FileInput fileSelector={AssetSelector}
                              metadataEditor={AssetMetadataEditor}
                              previewImage
                              className={mdClass}
                              key={field}
                              {...props} />
        case 'gallery':
            return <GalleryInput fileSelector={AssetSelector}
                                 metadataEditor={AssetMetadataEditor}
                                 className={mdClass}
                                 key={field}
                                 {...props} />
        case 'file':
            return <FileInput fileSelector={AssetSelector}
                              metadataEditor={AssetMetadataEditor}
                              download
                              className={mdClass}
                              key={field}
                              {...props} />
        case 'dropdown':
            return <DropDownInput options={props.column.options.split(',')}
                                  className={mdClass}
                                  key={field}
                                  {...props} />
        case 'lookup':
            return <LookupContainer className={mdClass} key={field}{...props}/>
        case 'multiselect':
            return <MultiSelectInput options={(options ?? '').split(',')}
                                     className={mdClass}
                                     key={field}
                                     {...props} />
        case 'treeSelect':
            return <TreeSelectContainer className={mdClass} 
                                        key={field} 
                                        {...props}/>
        default:
            return <TextInput className={mdClass} 
                              key={field} 
                              {...props}/>
    }
}

