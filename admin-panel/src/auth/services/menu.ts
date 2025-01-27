import useSWR from "swr";

import {fetcher, swrConfig} from "../../services/util";
import { Schema,MenuItem } from "../types/schema";
import { fullAuthApiUrl } from "../configs";

export function useTopMenuBar (): MenuItem[]{
    const { data} = useSWR<Schema>(fullAuthApiUrl('/schemas/name/top-menu-bar?type=menu'), fetcher, swrConfig)
    return data?.settings?.menu?.menuItems ?? [] as MenuItem[];
}