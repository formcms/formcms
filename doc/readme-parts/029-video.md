


---
## Video Processing Plugin
<details>
<summary>
FormCMS's video processing plugin converts MPEG files to HLS format, enabling seamless online video streaming.
</summary>

### Overview
The video processing plugin can be deployed as a standalone node for scalability or deployed to the same node as the web app for simplicity.

### Deployment Options

#### Distributed Deployment
```
Web Apps (n) ┌──→ NATS Message Broker ───→ Video Processing Apps (m)
             │                                 │
             └────────→ Cloud Storage ◄────────┘
```
- Multiple web apps send video processing requests via a NATS message broker.
- Video processing apps (scalable instances) convert videos and store outputs in cloud storage.

#### Single-Node Deployment
```
Web App ───→ Channel ───→ Video Processing Worker
   │                                     │
   └──────→ Storage (Local/Cloud) ◄──────┘
```
- A single web app communicates with a background worker via a channel.
- Processed videos are saved to local or cloud storage.

### Video Upload
Upload videos as you would any asset. When the server detects a video file, it triggers a processing event by sending a message.

### Video Processing
Upon receiving the message, the plugin:
1. Converts the MPEG file to an HLS-compliant `.m3u8` playlist and segmented video files.
2. Stores the output in cloud storage.

### Video Playback
Integrate videos into your site using the Grape.js Video component:
- Drag and drop the component into your layout.
- Set the source to `{{video_field_name.url}}` for seamless playback.

</details>

