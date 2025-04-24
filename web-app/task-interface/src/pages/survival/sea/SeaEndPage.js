import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../css/SurvivalItemsPage.css';

const SeaEndPage = () => {
    const navigate = useNavigate();
    const eventOrderIndex = parseInt(sessionStorage.getItem('eventOrderIndex'), 10) || 0;
    const eventOrder = JSON.parse(sessionStorage.getItem('eventOrder')) || [];
    const [nextButton, setNextButton] = useState('');

    const handleTaskClick = () => {
        const eventRoutes = {
          "Discussion 1": "/discussion-explanation",
          "Discussion 2": "/discussion-explanation-2",
          "Survival 1": "/desert-intro",
          "Survival 2": "/sea-intro"
        };

        if ((eventOrderIndex + 1) === 3) {
            navigate('/luna-instructions');
            sessionStorage.setItem('eventOrderIndex', eventOrderIndex + 1);
        } else if (eventOrder.length > eventOrderIndex) {
            navigate(eventRoutes[eventOrder[eventOrderIndex]]);
            sessionStorage.setItem('eventOrderIndex', eventOrderIndex + 1);
        } else {
            navigate('/discussion-explanation');
        }
    };

    useEffect(() => {
        if ((eventOrderIndex + 1) === 3) {
            setNextButton('Next');
        } else if ((eventOrderIndex + 1) === 4) {
            setNextButton('Task 2');
        } else {
            setNextButton(`Task ${eventOrderIndex + 1}`);
        };
    }, [eventOrderIndex]);

    // Check if all tasks are completed
    const allTasksCompleted = eventOrderIndex >= 3;

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

export default SeaEndPage;