import {Menu} from "primereact/menu";
import {MenuItem} from "primereact/menuitem";
import {useAssetMenuItems, useEntityMenuItems, useSystemMenuItems} from "../../lib/admin-panel-lib/useMenuItems";
import {configs} from "../config";
import {Logo} from "../layout/Logo";
import {GlobalStateKeys, useGlobalState} from "../globalState";
import {cnAssetMenuLabel, cnSystemMenuLabels} from "../types/menu";

export function Sidebar() {
    const [lan] = useGlobalState<string>( GlobalStateKeys.Language, 'en');
    const [_, setActiveMenu] = useGlobalState<MenuItem|null>( GlobalStateKeys.ActiveMenu, null);
    const entityMenuItems: MenuItem[] = useEntityMenuItems(configs.entityRouterPrefix);
    const assetMenuItems: MenuItem[] = useAssetMenuItems(configs.entityRouterPrefix, lan==='en' ? undefined: cnAssetMenuLabel);

    const systemMenuItems: MenuItem[] = useSystemMenuItems(
        configs.entityRouterPrefix,configs.authRouterPrefix,configs.auditLogRouterPrefix,configs.schemaBuilderRouter,
        lan === 'en' ? undefined : cnSystemMenuLabels
    );

    function appendSetActiveMenu(menuItems: MenuItem[]) {
        menuItems.forEach(item => {
            if (item.command) {
                const cmd = item.command;
                item.command = (e) =>{
                    setActiveMenu(item)
                    cmd(e)
                }
            };
        })
    }
    appendSetActiveMenu(entityMenuItems);
    appendSetActiveMenu(systemMenuItems);
    appendSetActiveMenu(assetMenuItems);

    let items: MenuItem[] = [
        {
            template: () => {
                return (
                    <span className="inline-flex align-items-center gap-1 px-2 py-2">
                        <Logo/> Form CMS
                    </span>
                )
            }
        },
        {separator:true},
        {
            label: lan === 'en'?'Entities':"数据",
            items: entityMenuItems,
        }
    ];
    if (assetMenuItems.length > 0) {
        items.push({separator: true})
        items.push({
            label:lan === 'en'? 'Assets': "资料",
            items: assetMenuItems,
        })
    }
    if (systemMenuItems.length > 0) {
        items.push({separator: true})
        items.push({
            label: lan === 'en'? 'System': "系统",
            items: systemMenuItems,
        })
    }

    return (
        <div className="card flex justify-content-center">
            <Menu model={items} className="w-full md:w-15rem" style={{fontSize: '1.1rem'}}/>
        </div>
    )
}