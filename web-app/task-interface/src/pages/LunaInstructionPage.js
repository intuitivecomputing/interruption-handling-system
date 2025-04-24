import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

const LunaInstructionPage = () => {
  const navigate = useNavigate();
  const [instructionText, setInstructionText] = useState('');
  const [mediaPath, setMediaPath] = useState('');
  const condition = sessionStorage.getItem('condition') || "";

  useEffect(() => {
    if (condition === 'B') {
        setInstructionText('If you ever want luna to stop talking, say "Luna" or "Stop".');
        setMediaPath('/media/luna-baseline.mp4');
    } else {
        setInstructionText('If you ever want luna to stop talking, say "Luna" or "Stop".');
        setMediaPath('/media/luna-experimental.mp4');
    }
  }, [condition]);

  const handleNextClick = () => {
    navigate("/practice-task");
  };

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>Welcome to Luna's Instruction Page</h1>
      <p style={styles.text}>Luna is a social robot that will assist you in completing two tasks throughout the study.</p>
      
      <div style={styles.mediaContainer}>
        <div style={styles.videoContainer}>
          <video width="300" controls style={styles.video}>
            <source src={mediaPath} type="video/mp4" />
          </video>
          <p style={styles.caption}>Example interaction with Luna</p>
        </div>
        <div style={styles.imageContainer}>
          <img
            src="/media/luna-processing.png"
            alt="Luna is processing"
            style={styles.robotImage}
          />
          <p style={styles.caption}>Luna is processing...</p>
        </div>
      </div>
      <p style={styles.textBold}>{instructionText}</p>
      <p style={styles.textSmall}>Press "Next" to begin your practice task</p>
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
    width: '300px',
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

export default LunaInstructionPage;