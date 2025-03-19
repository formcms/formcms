import {useRef, useState} from "react";
import {Message} from "primereact/message";
import {Toast} from "primereact/toast";

export function useCheckError(){
    const toast = useRef<any>(null);
    const [error, setError] = useState('')
    return {
        handleErrorOrSuccess : async (error :string, succeedMessage:string, cb:any) =>{
            if (error){
                setError(error)
            }else {
                toast.current.show({severity: 'success', summary:succeedMessage})
                if (cb) {
                    await new Promise(r => setTimeout(r, 500));
                    cb();
                }
            }
        },
        CheckErrorStatus: ()=>{
            return <>
                <Toast ref={toast} />
                {error&& error.split('\n').map(e =>(<><Message key={e} severity={'error'} text={e}/>&nbsp;&nbsp;</>))}
            </>
        }
    }


}