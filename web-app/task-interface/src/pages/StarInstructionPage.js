import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

const StarInstructionPage = () => {
    const navigate = useNavigate();
    const [instructionText, setInstructionText] = useState('');
    const [mediaPath, setMediaPath] = useState('');

    useEffect(() => {
        const conditionOrder = JSON.parse(sessionStorage.getItem('conditionOrder')) || [];

        // Logic to determine the instruction text based on event and condition orders
        if (conditionOrder[0] === 'Baseline') {
            setInstructionText('If you want to get Star\'s attention, say "Star".');
            setMediaPath('/media/star-baseline.mp4');
        } else {
            setInstructionText('Star will be always listening to you.');
            setMediaPath('/media/star-experimental.mp4');
        }
    }, []);

    const handleNextClick = () => {
        const eventOrder = JSON.parse(sessionStorage.getItem('eventOrder')) || [];
        let eventOrderIndex = sessionStorage.getItem('eventOrderIndex');

        const eventRoutes = {
            "Discussion 1": "/discussion-explanation",
            "Discussion 2": "/discussion-explanation-2",
            "Survival 1": "/desert-intro",
            "Survival 2": "/sea-intro"
        };

        if (eventOrder.length > 0) {
            navigate(eventRoutes[eventOrder[eventOrderIndex]]);
            eventOrderIndex = parseInt(eventOrderIndex, 10) + 1;
            sessionStorage.setItem('eventOrderIndex', eventOrderIndex);
        } else {
            navigate('/discussion-explanation'); // Default fallback
        }
    };

    return (
        <div style={styles.container}>
            <h1 style={styles.title}>Welcome to Star's Instruction Page</h1>
            <p style={styles.text}>Star is a social robot that will assist you in completing two tasks throughout the study.</p>

            <div style={styles.mediaContainer}>
                <div style={styles.videoContainer}>
                    <video width="300" controls style={styles.video}>
                        <source src={mediaPath} type="video/mp4" />
                    </video>
                    <p style={styles.caption}>Example interaction with Star</p>
                </div>
                <div style={styles.imageContainer}>
                    <img
                        src="/media/star-processing.png"
                        alt="Star is processing"
                        style={styles.robotImage}
                    />
                    <p style={styles.caption}>Star is processing...</p>
                </div>
            </div>
            <p style={styles.textBold}>{instructionText}</p>
            <p style={styles.textSmall}>Press "Next" to begin your task</p>
            <button style={styles.nextButton} onClick={handleNextClick}>
                Next
            </button>
        </div>
    );
};

const styles = {
    container: {
        fontFamily: 'Arial, sans-serif',
        textAlign: 'center',
        padding: '20px',
    },
    title: {
        fontSize: '3em',
        color: '#000',
    },
    text: {
        fontSize: '1.7em',
        color: '#000',
        marginTop: '50px',
    },
    textSmall: {
        fontSize: '1.4em',
        color: '#000',
        marginTop: '30px',
    },
    textBold: {
        fontWeight: 'bold',
        fontSize: '1.9em',
        color: '#000',
        marginTop: '40px',
    },
    mediaContainer: {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: '20px',
        margin: '20px 0',
        marginTop: '50px',
    },
    video: {
        width: '250px',
        height: 'auto',
        borderRadius: '10px',
    },
    videoContainer: {
        marginRight: '20px',
    },
    imageContainer: {
        textAlign: 'center',
    },
    robotImage: {
        width: '300px',
        height: 'auto',
        borderRadius: '10px',
    },
    caption: {
        fontSize: '1.6em',
        color: '#333',
        marginTop: '10px',
    },
    nextButton: {
        fontSize: '1.5rem',
        padding: '0.8rem 1.6rem',
        backgroundColor: '#61dafb',
        border: 'none',
        borderRadius: '5px',
        color: '#282c34',
        cursor: 'pointer',
        transition: 'background-color 0.3s ease',
        marginTop: '20px',
    },
};

export default StarInstructionPage;
