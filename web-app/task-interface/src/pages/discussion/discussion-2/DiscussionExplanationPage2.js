import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import '../DiscussionPage.css';
import io from 'socket.io-client';
import axios from 'axios';

const DiscussionExplanationPage2 = () => {
    useEffect(() => {
        const socket = io('http://localhost:4000');
        // Clean up the socket connection on component unmount
        return () => {
            socket.disconnect();
        };
    }, []);

    const navigate = useNavigate();
    const handleNextClick = () => {
        navigate('/discussion-task-2');
        axios.post('http://localhost:4000/started');
    };

    return (
        <div className="discussion">
            <div>
                <p className="subtitle">You will prepare a short presentation on the topic below. While you may take a stance in your presentation, you must also discuss the opposing perspective. If you choose, you can counter the opposing viewpoint.</p>
                <h1>Topic: "Should the federal government ban capital punishment? Is it an effective crime deterrent? Is it a fair form of justice, or does it cause more harm than good?"</h1>
                <p className="subtitle">Luna will assist you in preparing the presentation by engaging in a discussion about the topic with you. Before the discussion, we will give time to think about the topic. If you are ready, press "Next".</p>
                <button className="next-button" onClick={handleNextClick}>Next</button>
            </div>
        </div>
    );
};
export default DiscussionExplanationPage2;