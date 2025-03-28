import {GlobalStateKeys, useGlobalState} from "../globalState";
import {MenuItem} from "primereact/menuitem";
import React from "react";
import {Menubar} from "primereact/menubar";
import {MenuEnd} from "../layout/MenuEnd";

export function MenuBar() {
    const [activeMenu, _] = useGlobalState<MenuItem|null>( GlobalStateKeys.ActiveMenu, null);
    const start = activeMenu ? <h3><i className={activeMenu.icon}></i> {'    ' + activeMenu.label}</h3>:null;
    return  <Menubar start={start} end={<MenuEnd/>}/>
}