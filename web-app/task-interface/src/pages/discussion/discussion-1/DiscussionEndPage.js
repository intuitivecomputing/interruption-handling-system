import React, { useState, useEffect} from 'react';
import { useNavigate } from 'react-router-dom';
import '../DiscussionPage.css';
import CountdownTimer from '../../../components/CountdownTimer';

const DiscussionEndPage = () => {
    const navigate = useNavigate();
    const [submitted, setSubmitted] = useState(false);
    const eventOrderIndex = parseInt(sessionStorage.getItem('eventOrderIndex'), 10) || 0;
    const eventOrder = JSON.parse(sessionStorage.getItem('eventOrder')) || [];
    const [nextButton, setNextButton] = useState('');

    const handleDoneClick = () => {
        setSubmitted(true);
    };

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

    const allTasksCompleted = eventOrderIndex >= eventOrder.length;

    const handleTimeUp = () => {
        alert("Time is done");
        setSubmitted(true);
    };
    return (
        <div className="discussion">
            {submitted ? (
                <div className="thank-you">
                    <h1>Thank you for submitting!</h1>
                    <p className="instruc">Open the door when you are done with the task.</p>
                    {!allTasksCompleted && (
                    <>
                        <p className="instruc">Do not press the button below.</p>
                        <button onClick={handleTaskClick}>
                            {nextButton}
                        </button>
                    </>
                    )}
                </div>
            ) : (
                <>
                    <CountdownTimer duration={120} onTimeUp={handleTimeUp} />
                    <div className="title">
                        <h1>Now, you have two minutes to present about the topic to the camera.</h1>
                        <p className="subtitle">Press the "Done" button when you are done.</p>
                        <button className="next-button" onClick={handleDoneClick}>Done</button>
                    </div>
                </>
            )}
        </div>
    );
};
export default DiscussionEndPage;