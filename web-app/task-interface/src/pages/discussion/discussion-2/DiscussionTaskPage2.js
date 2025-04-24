import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import io from 'socket.io-client';
import axios from 'axios';
import '../DiscussionPage.css';

const DiscussionTaskPage2 = () => {
    const [done, setDone] = useState(false);
    const navigate = useNavigate();

    useEffect(() => {
        const socket = io('http://localhost:4000');
        // Clean up the socket connection on component unmount
        return () => {
            socket.disconnect();
        };
    }, []);

    const handleNextClick = async () => {
        navigate('/discussion-end-2');
    };

    const handleDoneClick = () => {
        axios.post('http://localhost:4000/finished');
        setDone(true);
    };

    return (
        <div className="discussion">
            {done ? (
                <>
                    <h1>Now you will make your short presentation.</h1>
                    <p className="subtitle">Press the "Start presentation" button to start the discussion.</p>
                    <button className="next-button" onClick={handleNextClick}>Start presentation</button>
                </>
            ) : (
                <>
                    <h1>Discussion</h1>
                    <p className="subtitle">Discuss with Luna about the topic below to prepare your short presentation.</p>
                    <div className="topic-box">
                        <p className="topic">Topic: "Should the federal government ban capital punishment? Is it an effective crime deterrent? Is it a fair form of justice, or does it cause more harm than good?"</p>
                    </div>
                    <button className="next-button" onClick={handleDoneClick}>Done</button>
                </>
            )}
        </div>
    );
};
export default DiscussionTaskPage2;