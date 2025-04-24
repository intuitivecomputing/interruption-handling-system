import React, { useState, useEffect } from 'react';
import io from 'socket.io-client';
import axios from 'axios';
import '../css/SurvivalItemsPage.css';
import CountdownTimer from '../../../components/CountdownTimer';
import { useNavigate } from 'react-router-dom';

const SeaTaskPage = () => {
    const navigate = useNavigate();
    const [robotName, setRobotName] = useState('');

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
    const [error, setError] = useState('');
    const [isTimeUp, setIsTimeUp] = useState(false);
    const nameToAbbreviation = {
        'sextant': 'sextant',
        'shaving mirror': 'mirror',
        'mosquito netting': 'netting',
        '5 liter container of water': 'water',
        'case of army rations': 'armyRations',
        'maps of the Atlantic Ocean': 'map',
        'floating seat cushion': 'cushion',
        '10 liter can of oil/petrol mixture': 'oil',
        'small transistor radio': 'radio',
        '20 square feet of opaque plastic sheeting': 'plasticSheeting',
        'a can of shark repellent': 'sharkRepellent',
        'a bottle of 160 proof rum': 'rum',
        '15 feet of nylon rope': 'rope',
        '1 box of chocolate bars': 'bars',
        'an ocean fishing kit & pole': 'fishingKit'
    };
    
    useEffect(() => {
        const eventOrderIndex = JSON.parse(sessionStorage.getItem('eventOrderIndex')) || 0;
        
        if (eventOrderIndex === 1 || eventOrderIndex === 2) {
            setRobotName('Star');
        } else {
            setRobotName('Luna');
        }

        const socket = io('http://localhost:4000');

        console.log('Connecting to server...');
        axios.post('http://localhost:4000/started');
        // Listen for real-time updates
        socket.on('connect', () => {
            console.log('Connected to server');
        });
        socket.on('update-list', (data) => {
            console.log('Received update list from server:', data);
            if (data && Array.isArray(data)) {
                setSortedList(data);
            } else {
                console.error('Invalid update list data:', data);
            }
        });
        return () => {
            socket.off('connect');
            socket.off('update-list');
        };
    }, []);

    const handleTimeUp = () => {
        if (!isTimeUp) {
            handleDoneClick(true);
            alert("Time is done");
            setIsTimeUp(true);
        }
    };

    const handleClickItem = (item) => {
        if (isTimeUp) return; // Prevent interaction if time is up
        const firstEmptyIndex = sortedList.indexOf(null);
        if (firstEmptyIndex !== -1) {
            const updatedList = [...sortedList];
            updatedList[firstEmptyIndex] = item;
            setSortedList(updatedList);
            setError(''); // Clear any existing error
            // Convert the updated list to a semicolon-separated string of abbreviations
            const abbreviatedList = updatedList.map(item => item ? nameToAbbreviation[item] : 'null').join(';');
            console.log('Sending updated list to server:', abbreviatedList);
            axios.post('http://localhost:4000/update_item_list', { message: abbreviatedList })
                .catch(error => {
                    console.error('There was an error updating the sorted list!', error);
                });
        } else {
            setError('All slots are filled. Remove an item before adding another.');
        }
    };
    const handleRemoveItem = (index) => {
        if (isTimeUp) return; // Prevent interaction if time is up
        const updatedList = [...sortedList];
        updatedList[index] = null;
        setSortedList(updatedList);
        setError('');
        // Convert the updated list to a semicolon-separated string of abbreviations
        const abbreviatedList = updatedList.map(item => item ? nameToAbbreviation[item] : 'null').join(';');
        console.log('Removing item from sorted list and sending update to server:', abbreviatedList);
        axios.post('http://localhost:4000/update_item_list', { message: abbreviatedList })
            .catch(error => {
                console.error('There was an error updating the sorted list!', error);
            });
    };
    const handleDoneClick = (isTimeUpOverride = false) => {
        if (!isTimeUpOverride && !isTimeUp) {
            if (sortedList.includes(null)) {
                setError('Please fill all the items before submitting.');
                return;
            }
            if (window.confirm('Are you sure you want to submit the final item list?')) {
                // Submit the sorted list
                axios.post('http://localhost:4000/finished');
                console.log('Submitting final item list:', sortedList);
                navigate('/sea-end')
            }
        } else {
            axios.post('http://localhost:4000/finished');
            console.log('Submitting final item list:', sortedList);
            navigate('/sea-end')
        }
    };
    const closeErrorModal = () => {
        setError('');
    };
    return (
        <div className="item-selection">
          <h1 className="items-title">Sea Survival</h1>
            {error && (
                <div className="modal">
                    <div className="modal-content">
                        <span className="close-button" onClick={closeErrorModal}>&times;</span>
                        <p>{error}</p>
                    </div>
                </div>
            )}
            <p className='subtitle'>Discuss with {robotName} to choose 7 most important items for survivial.</p>
            <CountdownTimer duration={300} onTimeUp={handleTimeUp} />
            <div className="object-lists">
                <div className="object-list">
                    {objects.slice(0, 7).map((item, index) => (
                        !sortedList.includes(item) && (
                            <div
                                key={index}
                                className={`object-item ${sortedList.includes(item) ? 'disabled' : ''}`}
                                onClick={() => handleClickItem(item)}
                                style={{ pointerEvents: isTimeUp ? 'none' : 'auto' }} // Disable click if time is up
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
                            onClick={() => item && handleRemoveItem(index)}
                            style={{ pointerEvents: isTimeUp ? 'none' : 'auto' }} // Disable click if time is up
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
                                className={`object-item ${sortedList.includes(item) ? 'disabled' : ''}`}
                                onClick={() => handleClickItem(item)}
                                style={{ pointerEvents: isTimeUp ? 'none' : 'auto' }} // Disable click if time is up
                            >
                                {item}
                            </div>
                        )
                    ))}
                </div>
            </div>
            <button
                className="done-button"
                onClick={() => handleDoneClick()}
                style={{ pointerEvents: isTimeUp ? 'none' : 'auto' }} // Disable click if time is up
            >
                Done
            </button>
        </div>
    );
};
export default SeaTaskPage;