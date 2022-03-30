import "bootstrap/dist/css/bootstrap.css";

import { render } from "preact";
import App from "./app/App";

const rootElement = document.getElementById("app");

render(<App />, rootElement!);
