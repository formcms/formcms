import {fetchMe} from "../services/userService.js";

let user; 
let currentUserPromise;

export function getUser() {
    return user;
}

export function ensureUser(){
    if (user) {
        return user;
    }

    const proceed = confirm("You must log in to perform this action. Do you want to log in now?");
    if (proceed) {
        window.location.href = "/portal?ref=" + encodeURIComponent(window.location.href);
    }
    return false; 
}

//single flight
export async function fetchUser() {
    if (user) return;
    if (currentUserPromise) {
        return currentUserPromise;
    }

    try {
        currentUserPromise = fetchMe();
        user = await currentUserPromise;
        return user;
    } catch (error) {
        console.error('API call failed:', error);
        return false;
    } finally {
        // Clear the promise after completion
        currentUserPromise = null;
    }
}