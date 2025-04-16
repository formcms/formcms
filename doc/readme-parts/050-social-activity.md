



---

## Social Activity
<details>  
<summary>  
The Social Activity feature enhances user engagement by enabling views, likes, saves, and shares. It also provides detailed analytics to help understand content performance.
</summary>  

### Endpoints
- `GET /api/activities/{entityName}/{recordId:long}`  
  Increments the view count by 1. Returns the active status and count for: like, view, share, and save.

- `GET /api/activities/record/{entityName}/{recordId}?type={view|share}`  
  Retrieves activity info of type `view` or `share` for a given entity record.

- `POST /api/activities/toggle/{entityName}/{recordId}?type={like|save}&active={true|false}`  
  Toggles the activity (like or save) on or off based on the `active` flag.

### Challenges
The system cannot leverage traditional output caching due to dynamic nature of the content, which may lead to high database load under heavy traffic.

To address this, buffered writes are introduced. Activity events are first stored in a buffer (in-memory or Redis), and then periodically flushed to the database, balancing performance and accuracy.

---

### Load Testing

Below is a test script using [k6](https://k6.io/) to simulate traffic and measure performance:

```javascript
import http from 'k6/http';
import { check } from 'k6';
import { Trend } from 'k6/metrics';

const ResponseTime = new Trend('response_time', true);

export const options = {
    stages: [
        { duration: '30s', target: 300 },
        { duration: '30s', target: 300 },
        { duration: '30s', target: 0 },
    ],
    thresholds: {
        'http_req_failed': ['rate<0.01'],
        'http_req_duration': ['p(95)<500'],
        'response_time': ['p(95)<500'],
    },
};

export default function () {
    const id = Math.floor(Math.random() * 100) + 1;
    const res = http.get(`http://localhost:5000/api/activities/post/${id}`);
    ResponseTime.add(res.timings.duration);
    check(res, { 'status is 200': (r) => r.status === 200 });
}
```

---

### Performance Comparison

#### No Buffer

- ✅ Simple to deploy and debug
- ❌ High database load under heavy traffic
- ⏱ Avg. response time: **35.28ms**
- 🧪 Total requests: **509,762**
- 📉 Throughput: ~**5,664 req/s**

#### Redis Buffer

- ✅ High performance
- ✅ Scalable across instances
- ❌ More complex infrastructure (requires Redis setup)
- ⏱ Avg. response time: **11.78ms**
- 🧪 Total requests: **1,520,072**
- 📉 Throughput: ~**16,889 req/s**

#### Memory Buffer

- ✅ Highest performance
- ✅ Easy to deploy
- ❌ Not horizontally scalable (buffer is local to instance)
- ⏱ Avg. response time: **4ms**
- 🧪 Total requests: **4,132,111**
- 📉 Throughput: ~**45,912 req/s**

---

### Summary

Each buffering strategy has its tradeoffs:

| Strategy       | Performance | Scalability | Complexity | Avg Response Time |
|----------------|-------------|-------------|------------|-------------------|
| No Buffer      | Medium      | High        | Low        | ~35ms             |
| Memory Buffer  | High        | Low         | Low        | ~4ms              |
| Redis Buffer   | High        | High        | Medium     | ~12ms             |

Choose the approach based on your system’s scalability requirements and infrastructure constraints.

</details>