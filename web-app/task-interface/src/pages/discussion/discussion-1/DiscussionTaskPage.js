import React, { useState, useEffect } from 'react';
import io from 'socket.io-client';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import '../DiscussionPage.css';

const DiscussionTaskPage = () => {
    const [next, setNext] = useState(false);
    const navigate = useNavigate();
    const [robotName, setRobotName] = useState('');

    useEffect(() => {
        const eventOrderIndex = JSON.parse(sessionStorage.getItem('eventOrderIndex')) || 0;
        
        if (eventOrderIndex === 1 || eventOrderIndex === 2) {
            setRobotName('Star');
        } else {
            setRobotName('Luna');
        }

        const socket = io('http://localhost:4000');
        // Clean up the socket connection on component unmount
        return () => {
            socket.disconnect();
        };
    }, []);

    const handleNextClick = async () => {
        setNext(true);
        try {
            await axios.post('http://localhost:4000/started');
        } catch (error) {
            console.error('Error posting to server:', error);
        }
    };

    const handleDoneClick = () => {
        axios.post('http://localhost:4000/finished');
        navigate('/discussion-end');
    };

    return (
        <div className="discussion">
            {next ? (
                <>
                    <h1>Discussion</h1>
                    <p className="subtitle">Discussion with {robotName} about the topic to prepare your 2-minute presentation.</p>
                    <div className="topic-box">
                        <p className="topic">Topic: "Should the use of AI language models like ChatGPT be allowed and encouraged in educational settings to enhance learning and teaching, or does their integration pose significant risks to academic integrity and critical thinking skills?"</p>
                    </div>
                    <button className="next-button" onClick={handleDoneClick}>Done</button>
                </>
            ) : (
                <>
                    <h1>Now you will discuss with {robotName} about the topic.</h1>
                    <p className="subtitle">Press the "Start discussion" button to start the discussion.</p>
                    <p className="subtitle">After the discussion, you will be given 2 minutes to give the presentation about the topic.</p>
                    <button className="next-button" onClick={handleNextClick}>Start discussion</button>
                </>
            )}
        </div>
    );
};
export default DiscussionTaskPage; 