import React, { useState, useEffect } from 'react';
import io from 'socket.io-client';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';

const PracticeTaskPage = () => {
  const navigate = useNavigate();
  const [taskCompleted, setTaskCompleted] = useState(false);
  const [answers, setAnswers] = useState({
    q1: '',
    q2: '',
    q3: '',
    q4: ''
  });

    useEffect(() => {
        const socket = io('http://localhost:4000');

        console.log('Connecting to server...');
        axios.post('http://localhost:4000/started');
        // Listen for real-time updates
        socket.on('connect', () => {
            console.log('Connected to server');
        });

        return () => {
            socket.off('connect');
        };
    }, []);

  const handleAnswerChange = (question, value) => {
    setAnswers(prev => ({ ...prev, [question]: value }));
  };

  const handleSubmitClick = () => {
    setTaskCompleted(true);
    console.log(answers); // You can handle the submission of answers here
  };

  const handleNextClick = () => {
    const taskOrder = JSON.parse(sessionStorage.getItem('taskOrder')) || [];
    let taskOrderIndex = sessionStorage.getItem('taskOrderIndex');

    const eventRoutes = {
      "Discussion": "/discussion-explanation-2",
      "Survival": "/desert-intro",
    };

    navigate(eventRoutes[taskOrder[taskOrderIndex]]);
  };

  return (
    <div style={styles.container}>
      {taskCompleted ? (
        <>
          <h1 style={styles.title}>Thank you for submitting!</h1>
            <p style={styles.text}>Do not press the button below.</p>
            <button style={styles.nextButton} onClick={handleNextClick}>
              Next
            </button>
        </>
      ) : (
        <>
          <h1 style={styles.title}>Practice Task</h1>
          <p style={styles.text}>Talk with Luna to answer the following questions:</p>

          <div style={styles.questionBlock}>
            <p style={styles.question}>1. What is the first spacecraft that successfully landed on the Moon?</p>
            <ul style={styles.answers}>
              <li><label><input type="radio" name="q1" value="A" onChange={() => handleAnswerChange('q1', 'A')} /> A) Sputnik 1</label></li>
              <li><label><input type="radio" name="q1" value="B" onChange={() => handleAnswerChange('q1', 'B')} /> B) Apollo 11</label></li>
              <li><label><input type="radio" name="q1" value="C" onChange={() => handleAnswerChange('q1', 'C')} /> C) Luna 2</label></li>
              <li><label><input type="radio" name="q1" value="D" onChange={() => handleAnswerChange('q1', 'D')} /> D) Vostok 1</label></li>
            </ul>
          </div>

          <div style={styles.questionBlock}>
            <p style={styles.question}>2. Who was the first human to walk on the Moon?</p>
            <ul style={styles.answers}>
              <li><label><input type="radio" name="q2" value="A" onChange={() => handleAnswerChange('q2', 'A')} /> A) Yuri Gagarin</label></li>
              <li><label><input type="radio" name="q2" value="B" onChange={() => handleAnswerChange('q2', 'B')} /> B) Neil Armstrong</label></li>
              <li><label><input type="radio" name="q2" value="C" onChange={() => handleAnswerChange('q2', 'C')} /> C) Buzz Aldrin</label></li>
              <li><label><input type="radio" name="q2" value="D" onChange={() => handleAnswerChange('q2', 'D')} /> D) Michael Collins</label></li>
            </ul>
          </div>

          <div style={styles.questionBlock}>
            <p style={styles.question}>3. What year did the first human moon landing occur?</p>
            <ul style={styles.answers}>
              <li><label><input type="radio" name="q3" value="A" onChange={() => handleAnswerChange('q3', 'A')} /> A) 1965</label></li>
              <li><label><input type="radio" name="q3" value="B" onChange={() => handleAnswerChange('q3', 'B')} /> B) 1967</label></li>
              <li><label><input type="radio" name="q3" value="C" onChange={() => handleAnswerChange('q3', 'C')} /> C) 1969</label></li>
              <li><label><input type="radio" name="q3" value="D" onChange={() => handleAnswerChange('q3', 'D')} /> D) 1971</label></li>
            </ul>
          </div>

          <div style={styles.questionBlock}>
            <p style={styles.question}>4. How many people have walked on the Moon?</p>
            <ul style={styles.answers}>
              <li><label><input type="radio" name="q4" value="A" onChange={() => handleAnswerChange('q4', 'A')} /> A) 6</label></li>
              <li><label><input type="radio" name="q4" value="B" onChange={() => handleAnswerChange('q4', 'B')} /> B) 8</label></li>
              <li><label><input type="radio" name="q4" value="C" onChange={() => handleAnswerChange('q4', 'C')} /> C) 10</label></li>
              <li><label><input type="radio" name="q4" value="D" onChange={() => handleAnswerChange('q4', 'D')} /> D) 12</label></li>
            </ul>
          </div>

          {/* <div style={styles.questionBlock}>
            <p style={styles.question}>5. What are the names of the Apollo missions that successfully landed on the Moon?</p>
            <ul style={styles.answers}>
              <li><label><input type="radio" name="q5" value="A" onChange={() => handleAnswerChange('q5', 'A')} /> A) Apollo 9, 10, 11, 12, 13, 14</label></li>
              <li><label><input type="radio" name="q5" value="B" onChange={() => handleAnswerChange('q5', 'B')} /> B) Apollo 11, 12, 14, 15, 16, 17</label></li>
              <li><label><input type="radio" name="q5" value="C" onChange={() => handleAnswerChange('q5', 'C')} /> C) Apollo 7, 8, 9, 10, 11, 12</label></li>
              <li><label><input type="radio" name="q5" value="D" onChange={() => handleAnswerChange('q5', 'D')} /> D) Apollo 11, 13, 14, 15, 16, 17</label></li>
            </ul>
          </div>

          <div style={styles.questionBlock}>
            <p style={styles.question}>6. What is the largest planet in our solar system?</p>
            <ul style={styles.answers}>
              <li><label><input type="radio" name="q6" value="A" onChange={() => handleAnswerChange('q6', 'A')} /> A) Earth</label></li>
              <li><label><input type="radio" name="q6" value="B" onChange={() => handleAnswerChange('q6', 'B')} /> B) Jupiter</label></li>
              <li><label><input type="radio" name="q6" value="C" onChange={() => handleAnswerChange('q6', 'C')} /> C) Saturn</label></li>
              <li><label><input type="radio" name="q6" value="D" onChange={() => handleAnswerChange('q6', 'D')} /> D) Neptune</label></li>
            </ul>
          </div> */}

          <button style={styles.nextButton} onClick={handleSubmitClick}>
            Submit
          </button>
        </>
      )}
    </div>
  );
};

const styles = {
  container: {
    fontFamily: 'Arial, sans-serif',
    maxWidth: '600px',
    margin: '0 auto',
    padding: '20px',
  },
  title: {
    fontSize: '2em',
    color: '#333',
    marginBottom: '20px',
  },
  text: {
    fontSize: '1.2em',
    margin: '20px 0',
  },
  questionBlock: {
    margin: '20px 0',
    textAlign: 'left',
  },
  question: {
    fontSize: '1.2em',
    fontWeight: 'bold',
  },
  answers: {
    fontSize: '1em',
    listStyleType: 'none',
    paddingLeft: 0,
    lineHeight: '1.8em',
  },
  nextButton: {
    padding: '10px 20px',
    fontSize: '1.2em',
    cursor: 'pointer',
    backgroundColor: '#007BFF',
    color: '#fff',
    border: 'none',
    borderRadius: '5px',
    marginTop: '20px',
    justifyContent: 'center',
  },
};

export default PracticeTaskPage;
