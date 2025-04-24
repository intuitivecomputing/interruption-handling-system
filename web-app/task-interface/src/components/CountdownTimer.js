import React, { useState, useEffect } from 'react';

const CountdownTimer = ({ duration, onTimeUp }) => {
  const [timeLeft, setTimeLeft] = useState(duration);

  useEffect(() => {
    setTimeLeft(duration);
  }, [duration]);

  useEffect(() => {
    if (timeLeft === 0) {
      onTimeUp();
      return;
    }

    const timerId = setInterval(() => {
      setTimeLeft((prevTime) => prevTime - 1);
    }, 1000);

    return () => clearInterval(timerId);
  }, [timeLeft, onTimeUp]);

  const formatTime = (seconds) => {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}:${remainingSeconds < 10 ? '0' : ''}${remainingSeconds}`;
  };

  const calculateProgress = () => {
    return (timeLeft / duration) * 100; // Calculate the progress percentage
  };

  return (
    <div style={styles.container}>
      <div style={styles.timer}>{formatTime(timeLeft)}</div>
      <div style={styles.progressBarBackground}>
        <div style={{ ...styles.progressBar, width: `${calculateProgress()}%` }}></div>
      </div>
    </div>
  );
};

const styles = {
  container: {
    position: 'absolute',
    top: '10px',
    right: '10px',
    fontSize: '20px',
    textAlign: 'center',
    padding: '10px',
    borderRadius: '10px',
    backgroundColor: '#f2f2f2',
    boxShadow: '0 0 10px rgba(0, 0, 0, 0.1)',
    margin: '20px'
  },
  timer: {
    marginBottom: '10px',
    fontWeight: 'bold',
    fontSize: '40px'
  },
  progressBarBackground: {
    width: '100%',
    height: '10px',
    backgroundColor: '#ddd',
    borderRadius: '5px'
  },
  progressBar: {
    height: '10px',
    backgroundColor: '#4caf50',
    borderRadius: '5px'
  }
};

export default CountdownTimer;
