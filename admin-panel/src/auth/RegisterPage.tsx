import React from "react";
import {Card} from "primereact/card";
import {Link} from "react-router-dom";
import {BaseRouterProps} from "../../lib/admin-panel-lib/auth/AccountRouter";
import {InputText} from "primereact/inputtext";
import {Password} from "primereact/password";
import {Button} from "primereact/button";
import {useRegisterPage} from "../../lib/admin-panel-lib/auth/pages/useRegisterPage";

export function RegisterPage({baseRouter}:BaseRouterProps) {
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

    const {errors,success,loginLink,
        email,setEmail,
        password,setPassword,
        confirmPassword,setConfirmPassword,
        handleRegister
    } = useRegisterPage(baseRouter)
    return (
        <div style={containerStyle}>
            <Card title="Register" className="p-shadow-5" style={cardStyle}>
                <div className="p-fluid">
                    {errors.map(error=> (<div className="p-field"> <span className="p-error">{error}</span> </div>)) }
                    {success ? (
                        <div className="p-field">
                            <span className="p-message ">
                                Registration succeeded. <Link to={loginLink}>Click here to go to login</Link>
                            </span>
                        </div>
                    ) : (<>
                        <div className="p-field">
                            <label htmlFor="username">Email</label>
                            <InputText
                                id="username"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                            />
                        </div>
                        <div className="p-field">
                            <label htmlFor="password">Password</label>
                            <Password toggleMask
                                      id="password"
                                      value={password}
                                      onChange={(e) => setPassword(e.target.value)}
                                      feedback={false}
                            />
                        </div>
                        <div className="p-field">
                            <label htmlFor="confirmPassword">Confirm Password</label>
                            <Password toggleMask
                                      id="confirmPassword"
                                      value={confirmPassword}
                                      onChange={(e) => setConfirmPassword(e.target.value)}
                                      feedback={false}
                            />
                        </div>
                        <Button
                            label="Register"
                            icon="pi pi-check"
                            onClick={handleRegister}
                            className="p-mt-2"
                        />
                        <div className="p-mt-3">
                            <Link to={loginLink}>Already have an account? Login</Link>
                        </div>
                    </>)
                    }
                </div>
            </Card>
        </div>
    );
}