


---


## Popular Score
<details>  
<summary>  
The Popular Score is a dynamic metric that measures the engagement level of content record
</summary>  

The **Popular Score** is a simple way to measure how engaging content (like posts, videos, or articles) is based on user interactions such as **views**, **likes**, **shares**, and **how long ago the content was posted**. It helps platforms rank and promote content that’s getting attention, making it more visible on homepages, trending sections, or personalized feeds.

---

### Calculation of the Popular Score
The Popular Score combines views, likes, shares, and time since posting, with each part weighted to reflect its importance. Newer content gets a boost, while older content loses a bit of its score.

Scores are updated frequently (e.g., every minute) and stored in a fast system like Redis to keep things running smoothly.
To favor fresh content, the score is slightly reduced for older posts. The older the content, the more its score drops.
---

### How It’s Used
The Popular Score powers key features:    
1. **Ranking Content**: Shows the most engaging posts on homepages or trending pages.    
2. **Personalized Feeds**: Combines scores with user interests to suggest content.    
3. **Creator Insights**: Helps creators see what’s working to improve their posts.    
4. **Content Moderation**: Flags high-scoring content for review to ensure it follows platform rules. 

---

### Making It Work Smoothly
To handle lots of interactions without slowing down:
- **Batching**: Group interaction counts (views, likes, shares) to reduce database work.
- **Quick Updates**: Only update scores for new interactions to save time.
- **Fast Storage**: Use Redis to store scores for instant access.
- **Background Processing**: Update scores in the background to keep the platform responsive.

</details>