import http from 'k6/http';
import { check, sleep } from 'k6';

// k6 options for high QPS
export const options = {
  stages: [
    { duration: '30s', target: 10 }, // Ramp up to 1000 VUs over 30s
    { duration: '30s', target: 50 }, // Ramp up to 2000 VUs over 30s
    { duration: '30s', target: 100 }, // Ramp up to 3000 VUs over 30s
    { duration: '1m', target: 100 }, // Hold at 3000 VUs for 1 minute
    { duration: '30s', target: 0 }, // Ramp down to 0 VUs
  ],
  insecureSkipTLSVerify: true, // Skip TLS verification for localhost
};

// GraphQL endpoint
const endpoint = 'https://localhost:5001/api/graphql';

// GraphQL query template
const queryTemplate = `
query MyQuery($skip: Int!) {
  post(first: 20, skip: $skip) {
    displayText
    contentItemId
     post {
      tags {
        contentItems {
          contentItemId
          displayText
        }
      }
      categories {
        contentItems {
          contentItemId
          displayText
        }
      }
    }
  }
}
`;

export default function () {
  for(let i = 0; i < 10; i++){
    queryPost(i * 20);
  }
}

function queryPost(skip){
  // Prepare GraphQL request payload
  const payload = JSON.stringify({
    query: queryTemplate,
    variables: { skip: skip },
  });

  // Headers for the request
  const headers = {
    'Content-Type': 'application/json',
  };

  // Send POST request to GraphQL endpoint
  const res = http.post(endpoint, payload, { headers });

  // Check response
  check(res, {
    [`Status is 200 (skip: ${skip})`]: (r) => r.status === 200,
  });
}

/*
checks.........................: 100.00% 5760 out of 5760
     data_received..................: 61 MB   322 kB/s
     data_sent......................: 3.1 MB  16 kB/s
     http_req_blocked...............: avg=524.7µs  min=0s      med=0s     max=50.78ms p(90)=1µs     p(95)=1µs
     http_req_connecting............: avg=7.59µs   min=0s      med=0s     max=4.25ms  p(90)=0s      p(95)=0s
     http_req_duration..............: avg=2.03s    min=67.26ms med=2.05s  max=2.47s   p(90)=2.23s   p(95)=2.3s
       { expected_response:true }...: avg=2.03s    min=67.26ms med=2.05s  max=2.47s   p(90)=2.23s   p(95)=2.3s
     http_req_failed................: 0.00%   0 out of 5760
     http_req_receiving.............: avg=262.22µs min=15µs    med=91µs   max=12.54ms p(90)=559.1µs p(95)=954.04µs
     http_req_sending...............: avg=39.11µs  min=9µs     med=18µs   max=10.69ms p(90)=68µs    p(95)=99µs
     http_req_tls_handshaking.......: avg=513.35µs min=0s      med=0s     max=50.32ms p(90)=0s      p(95)=0s
     http_req_waiting...............: avg=2.03s    min=67.12ms med=2.05s  max=2.47s   p(90)=2.23s   p(95)=2.3s
     http_reqs......................: 5760    30.158167/s
     iteration_duration.............: avg=20.35s   min=12.89s  med=20.58s max=21.89s  p(90)=21.5s   p(95)=21.66s
     iterations.....................: 576     3.015817/s
     vus............................: 5       min=1            max=100
     vus_max........................: 100     min=100          max=100
*/
