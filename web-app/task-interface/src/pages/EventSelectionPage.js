import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const EventSelectionPage = () => {
  const navigate = useNavigate();
  const [selectedTaskOrder, setSelectedTaskOrder] = useState("");
  const [selectedCondition, setSelectedCondition] = useState("");

  const taskOrderOptions = [
    ["Survival", "Discussion"],
    ["Discussion", "Survival"],
  ];
  const conditionOptions = ["E", "B"];

  const handleTaskOrderChange = (event) => {
    setSelectedTaskOrder(event.target.value);
  };

  const handleConditionChange = (event) => {
    setSelectedCondition(event.target.value);
  };

  const handleNextClick = () => {
    // Store the selected task order and condition in session storage
    sessionStorage.setItem('taskOrder', JSON.stringify(taskOrderOptions[selectedTaskOrder]));
    sessionStorage.setItem('taskOrderIndex', 0);
    sessionStorage.setItem('condition', selectedCondition);
    navigate('/luna-instructions');
  };

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>Select Task Order</h1>
      <select style={styles.select} value={selectedTaskOrder} onChange={handleTaskOrderChange}>
        <option value="" disabled>Select Task Order</option>
        {taskOrderOptions.map((order, index) => (
          <option key={index} value={index}>
            {order.join(" → ")}
          </option>
        ))}
      </select>

      <h1 style={styles.title}>Select Condition</h1>
      <select style={styles.select} value={selectedCondition} onChange={handleConditionChange}>
        <option value="" disabled>Select Condition</option>
        {conditionOptions.map((condition, index) => (
          <option key={index} value={condition}>
            {condition}
          </option>
        ))}
      </select>

      <button style={styles.nextButton} onClick={handleNextClick} disabled={selectedTaskOrder === "" || selectedCondition === ""}>
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
    color: '#333',
  },
  text: {
    fontSize: '1.2em',
    margin: '20px 0',
  },
  select: {
    padding: '10px',
    fontSize: '1.2em',
    marginBottom: '20px',
  },
  nextButton: {
    padding: '10px 20px',
    fontSize: '1.2em',
    cursor: 'pointer',
    justifyContent: 'center',
  },
};

export default EventSelectionPage;