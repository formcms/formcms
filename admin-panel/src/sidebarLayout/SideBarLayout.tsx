import {Sidebar} from "./Sidebar";
import {AppRouters} from "../AppRouters";
import React from "react";
import {MenuBar} from "./MenuBar";

export function SidebarLayout() {
    return <div className="flex">
        <Sidebar/>
        <div style={{paddingLeft: '1rem', paddingRight: '1rem', width: '100%'}}>
            <MenuBar/>
            <AppRouters/>
        </div>
    </div>
}