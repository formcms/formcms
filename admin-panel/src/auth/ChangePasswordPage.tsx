import React from 'react';
import {Password} from 'primereact/password';
import {Button} from 'primereact/button';
import {Card} from 'primereact/card';
import {Link} from 'react-router-dom';
import 'primereact/resources/themes/saga-blue/theme.css';
import 'primereact/resources/primereact.min.css';
import 'primeicons/primeicons.css';
import {useChangePasswordPage} from "../../lib/admin-panel-lib/auth/pages/useChangePasswordPage";
import {GlobalStateKeys, useGlobalState} from "../globalState";

export const ChangePasswordPage: React.FC = () => {
    const [lan] = useGlobalState<string>( GlobalStateKeys.Language, 'en');

    const {
        errors, success,
        oldPassword, setOldPassword,
        password, setPassword,
        confirmPassword, setConfirmPassword,
        handleChangePassword
    } = useChangePasswordPage()

    const containerStyle: React.CSSProperties = {
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh',
        backgroundColor: '#f5f5f5',
    };

    const cardStyle: React.CSSProperties = {
        width: '300px',
    };

    return (
        <div style={containerStyle}>
            <Card title={lan === 'en' ?'Change Password': '更改密码'} className="p-shadow-5" style={cardStyle}>
                <div className="p-fluid">
                    {errors.map(error => (
                        <div className="p-field" key={error}><span className="p-error">{error}</span></div>))}
                    {success ? (
                        <div className="p-field">
                            <span className="p-message ">
                                Changing password succeeded. <Link to="/">Click here to go to Home Page</Link>
                            </span>
                        </div>
                    ) : (
                        <>
                            <div className="p-field">
                                <label htmlFor="oldPassword">{lan === 'en' ?'Password': '密码'}</label>
                                <Password
                                    id="username"
                                    value={oldPassword} toggleMask
                                    onChange={(e) => setOldPassword(e.target.value)}
                                />
                            </div>
                            <div className="p-field">
                                <label htmlFor="password">{lan === 'en' ?'New Password':'新密码'}</label>
                                <Password
                                    id="password"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    feedback={false} toggleMask
                                />
                            </div>
                            <div className="p-field">
                                <label htmlFor="confirmPassword">{lan === 'en' ?'Confirm New Password': '确认新密码'}</label>
                                <Password
                                    id="confirmPassword"
                                    value={confirmPassword} toggleMask
                                    onChange={(e) => setConfirmPassword(e.target.value)}
                                    feedback={false}
                                />
                            </div>
                            <Button
                                label={lan === 'en' ?'Submit': '提交'}
                                icon="pi pi-check"
                                onClick={handleChangePassword}
                                className="p-mt-2"
                            />
                        </>)
                    }
                </div>
            </Card>
        </div>
    );
};