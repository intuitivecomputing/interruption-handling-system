import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import '../css/SurvivalItemsPage.css';

const SeaInitialItemsPage = () => {
  const navigate = useNavigate();
  const [objects] = useState(
    ['sextant', 'shaving mirror', 
      'mosquito netting', '5 liter container of water', 
      'case of army rations', 'maps of the Atlantic Ocean', 
      'floating seat cushion', '10 liter can of oil/petrol mixture', 
      'small transistor radio', '20 square feet of opaque plastic sheeting', 
      'a can of shark repellent', 'a bottle of 160 proof rum', '15 feet of nylon rope', 
      '1 box of chocolate bars', 'an ocean fishing kit & pole']
  );
  const [sortedList, setSortedList] = useState(Array(7).fill(null));
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState('');
  const [robotName, setRobotName] = useState('');

  useEffect(() => {
    const eventOrderIndex = JSON.parse(sessionStorage.getItem('eventOrderIndex')) || 0;
    
    if (eventOrderIndex === 1 || eventOrderIndex === 2) {
        setRobotName('Star');
    } else {
        setRobotName('Luna');
    }
  },[]);

  const handleClickItem = (item) => {
    const itemIndex = sortedList.indexOf(item);
    if (itemIndex !== -1) {
      const updatedList = [...sortedList];
      updatedList[itemIndex] = null;
      setSortedList(updatedList);
    } else {
      const firstEmptyIndex = sortedList.indexOf(null);
      if (firstEmptyIndex !== -1) {
        const updatedList = [...sortedList];
        updatedList[firstEmptyIndex] = item;
        setSortedList(updatedList);
      } else {
        setError('All slots are filled. Remove an item before adding another.');
      }
    }
  };

  const handleDoneClick = () => {
    if (sortedList.includes(null)) {
      setError('Please fill all the items before submitting.');
      return;
    }

    if (window.confirm('Are you sure you want to submit the item list?')) {
      console.log('Submitting final sorted list:', sortedList);
      setSubmitted(true);
    }
  };

  const handleNextClick = () => {
    navigate('/sea-task');
  };

  const closeErrorModal = () => {
    setError('');
  };

  return (
    <div className="item-selection">
      {submitted ? (
        <div className="thank-you">
          <h1>Ready to discuss the sea survival simulation task with {robotName}?</h1>
          <p className='subtitle'>Now you and {robotName} have 5 minutes to decide on the final list of 7 most important items from the 15 items as a team.</p>
          <p className='subtitle'>Once you are ready, press the "Start timer" button to start the discussion. Press “Submit” when your team reaches an agreement. Please let the instructor know once you and {robotName} have finalized the list or the 5 minutes have passed by opening the door.</p>
          <button className="next-button" onClick={handleNextClick}>Start timer</button>
        </div>
      ) : (
        <>
          <h1 className="items-title">Sea Survival</h1>
          <p className="instruc">
            Before you discuss with {robotName}, please select the seven items that you think that are most important for sea survival from the list below. Click on an item to select it and it will be placed in the item list. Click on an item in the item list to remove it. Press the "Done" button when you have selected the items.
          </p>
          {error && (
            <div className="modal">
              <div className="modal-content">
                <span className="close-button" onClick={closeErrorModal}>&times;</span>
                <p>{error}</p>
              </div>
            </div>
          )}
          <div className="object-lists">
            <div className="object-list">
              {objects.slice(0, 7).map((item, index) => (
                !sortedList.includes(item) && (
                  <div
                    key={index}
                    className="object-item"
                    onClick={() => handleClickItem(item)}
                  >
                    {item}
                  </div>
                )
              ))}
            </div>
            <div className="sorted-list">
              {sortedList.map((item, index) => (
                <div
                  key={index}
                  className="placeholder"
                  onClick={() => item && handleClickItem(item)}
                >
                  {item || `Item ${index + 1}`}
                </div>
              ))}
            </div>
            <div className="object-list">
              {objects.slice(7).map((item, index) => (
                !sortedList.includes(item) && (
                  <div
                    key={index}
                    className="object-item"
                    onClick={() => handleClickItem(item)}
                  >
                    {item}
                  </div>
                )
              ))}
            </div>
          </div>
          <button className="done-button" onClick={handleDoneClick}>Done</button>
        </>
      )}
    </div>
  );
};

export default SeaInitialItemsPage;
