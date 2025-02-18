import axios from "axios";
import useSWR from "swr";
import {catchResponse, fetcher, swrConfig} from "../../services/util";
import {fullAuthApiUrl} from "../configs";
import { UserAccess } from "../types/userAccess";
import { ProfileDto } from "../types/profileDto";


export function useUserInfo() {
    return useSWR<UserAccess>(fullAuthApiUrl(`/profile/info`), fetcher, swrConfig)
}

export async function login(item:any) {
    return catchResponse(() => axios.post(fullAuthApiUrl(`/login?useCookies=true`), item));
}

export async function register(item:any) {
    return catchResponse(() => axios.post(fullAuthApiUrl(`/register`), item));
}

export async function changePassword(item:ProfileDto) {
    return catchResponse(() => axios.post(fullAuthApiUrl(`/profile/password`), item));
}
export async function logout() {
    return catchResponse(() => axios.get(fullAuthApiUrl(`/logout`)));
}
