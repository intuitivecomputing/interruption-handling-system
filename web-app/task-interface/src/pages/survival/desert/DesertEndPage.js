import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../css/SurvivalItemsPage.css';

const DesertEndPage = () => {
    const navigate = useNavigate();
    const taskOrderIndex = parseInt(sessionStorage.getItem('taskOrderIndex'), 10) || 0;
    const taskOrder = JSON.parse(sessionStorage.getItem('taskOrder')) || [];
    const [nextButton, setNextButton] = useState('');

    const handleTaskClick = () => {
        const eventRoutes = {
            "Discussion": "/discussion-explanation-2",
            "Survival": "/desert-intro",
        };

        navigate(eventRoutes[taskOrder[taskOrderIndex + 1]]);
        sessionStorage.setItem('taskOrderIndex', taskOrderIndex + 1);
    };
    
    useEffect(() => {
        if ((taskOrderIndex) === 0) {
            setNextButton('Task 2');
        } else {
            setNextButton("Done");
        };
    }, [taskOrderIndex]);
    
    // Check if all tasks are completed
    const allTasksCompleted = taskOrderIndex >= 1;

    return (
        <div className="item-selection">
            <div className="thank-you">
                <h1>Thank you for submitting!</h1>
                <p className="instruc">Open the door when you are done with the task.</p>
            </div>
            {!allTasksCompleted && (
                <>
                <p className="instruc">Do not press the button below.</p>
                <button onClick={handleTaskClick}>
                    {nextButton}
                </button>
                </>
            )}
        </div>
    );
};

export default DesertEndPage;