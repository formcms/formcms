import useSWR from "swr";
import {catchResponse, decodeError, fetcher, swrConfig} from "../../services/util";
import axios from "axios";
import {fullAuthApiUrl} from "../configs";
import { UserAccess } from "../types/userAccess";
import { RoleAccess } from "../types/roleAccess";

export  function useUsers(){
    let res = useSWR<UserAccess[]>(fullAuthApiUrl(`/accounts/users`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useRoles(){
    let res = useSWR<string[]>(fullAuthApiUrl(`/accounts/roles`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useEntities(){
    let res = useSWR<string[]>(fullAuthApiUrl(`/accounts/entities`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useSingleUser(id:string){
    let res = useSWR<UserAccess>(fullAuthApiUrl(`/accounts/users/${id}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useSingleRole(name:string){
    let res = useSWR<RoleAccess>(!name? null:fullAuthApiUrl(`/accounts/roles/${name}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function saveUser(formData:UserAccess){
    return catchResponse(()=>axios.post(fullAuthApiUrl(`/accounts/users`), formData))
}

export function deleteUser(id:string){
    return catchResponse(()=>axios.delete(fullAuthApiUrl(`/accounts/users/${id}`)))
}

export function saveRole(payload:RoleAccess){
    return catchResponse(()=>axios.post(fullAuthApiUrl(`/accounts/roles`), payload))
}

export function deleteRole(name:string){
    return catchResponse(()=>axios.delete(fullAuthApiUrl(`/accounts/roles/${name}`)))
}