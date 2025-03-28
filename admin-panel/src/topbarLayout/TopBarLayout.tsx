import {AppRouters} from "../AppRouters";
import React from "react";
import {TopMenuBar} from "./TopMenuBar";

export function TopBarLayout() {
    return <>
        <TopMenuBar/>
        <AppRouters/>
    </>
}