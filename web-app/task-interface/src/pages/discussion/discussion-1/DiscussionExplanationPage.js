import React, { useState , useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import '../DiscussionPage.css';
import CountdownTimer from '../../../components/CountdownTimer';
const DiscussionExplanationPage = () => {
    const navigate = useNavigate();
    const [isIntroPage, setIsIntroPage] = useState(true);
    const [robotName, setRobotName] = useState('');

    useEffect(() => {
        const eventOrderIndex = JSON.parse(sessionStorage.getItem('eventOrderIndex')) || 0;
        
        if (eventOrderIndex === 1 || eventOrderIndex === 2) {
            setRobotName('Star');
        } else {
            setRobotName('Luna');
        }
    }, []);

    const handleNextClick = () => {
        setIsIntroPage(false);
    };

    const handleDoneClick = () => {
        navigate('/discussion-task');
    };

    const handleTimeUp = () => {
        alert("Time is done");
        navigate('/discussion-task');
    };

    return (
        <div className="discussion">
            {isIntroPage ? (
                <div>
                    <p className="subtitle">You will be preparing a 2 minute presentation about the topic below.</p>
                    <h1>Topic: "Should the use of AI language models like ChatGPT be allowed and encouraged in educational settings to enhance learning and teaching, or does their integration pose significant risks to academic integrity and critical thinking skills?"</h1>
                    <p className="subtitle">{robotName} will assist you in preparing the presentation by engaging in a discussion about the topic with you. Before the discussion, we will give you two minutes to think about the topic. If you are ready, press "Start timer".</p>
                    <button className="next-button" onClick={handleNextClick}>Start timer</button>
                </div>
            ) : (
                <div className="discussion-content">
                    <CountdownTimer duration={120} onTimeUp={handleTimeUp} />
                    <div className="title">
                        <h1>Topic: "Should the use of AI language models like ChatGPT be allowed and encouraged in educational settings to enhance learning and teaching, or does their integration pose significant risks to academic integrity and critical thinking skills?"</h1>
                        <p className="subtitle">Take two minutes to carefully consider the topic. Press the "Done" button if you are ready to start the discussion with {robotName}.</p>
                        <button className="next-button" onClick={handleDoneClick}>Done</button>
                    </div>
                </div>
            )}
        </div>
    );
};
export default DiscussionExplanationPage;