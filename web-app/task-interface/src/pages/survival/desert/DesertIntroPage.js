import React, { useEffect, useState } from 'react';
import "../css/SurvivalIntroPage.css";

const DesertIntroPage = () => {
    const [currentPage, setCurrentPage] = useState(1);

    useEffect(() => {
        console.log("Current page is: ", currentPage);

        const timer = setTimeout(() => {
            if (currentPage === 1) {
                console.log("Transitioning to scene 2");
                setCurrentPage(2);
            } else {
                let nextPagePath = '/desert-explanation';
                window.location.assign(nextPagePath);
                console.log("Moving to explanation page");
            }
        }, 6000);

        return () => {
            console.log("Cleaning up");
            clearTimeout(timer);
        };
    }, [currentPage]);

    const backgroundStyle = {
        backgroundImage: currentPage === 1
            ? "url('media/desert-plane-crashing.png')"
            : "url('media/desert-plane.png')",
    };

    const textBoxStyle = {
        position: 'absolute',
        background: 'rgba(255, 255, 255, 0.5)',
        color: 'white',
        padding: '20px',
        backgroundColor: 'rgba(128, 128, 128, 0.5)',
        borderRadius: '10px',
        textShadow: '2px 2px 4px rgba(0, 0, 0, 0.3)',
        fontSize: '3rem',
        fontWeight: 'bold',
    };

    return (
        <div className="simulation-page" style={backgroundStyle}>
            {currentPage === 1 ? (
                <div>
                    <div style={{ ...textBoxStyle, top: '3%', left: '3%' }}>
                        <p>Mayday! Mayday! We've lost control and are going down!</p>
                    </div>
                    <div style={{ ...textBoxStyle, bottom: '3%', right: '3%' }}>
                        <p>Nearest settlement: Mining camp, 65 miles northwest</p>
                        </div>
                </div>

            ) : (
                 <div>
                    <div style={{ ...textBoxStyle, top: '3%', left: '3%' }}>
                        <p>10 AM, mid-August, blazing heat</p>
                    </div>
                    <div style={{ ...textBoxStyle, bottom: '3%', right: '3%' }}>
                        <p>Sonoran Desert, near the Mexico-USA border</p>
                    </div>
                </div>
            )}
        </div>
    );
};

export default DesertIntroPage;
