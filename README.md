# VainBot-Discord

This is a Discord bot that I use on a few servers for various tasks. It's built with [Discord.Net](https://github.com/RogueException/Discord.Net). The name `VainBot-Discord` annoys me so the repo is named `vbot-d`.

## Features

- Twitch stream notifications
- YouTube video notifications
- Twitter notifications
- Set reminders via chat command

## Setup

You'll need a Postgres database to use the bot as-is, though it uses EF Core so switching to a different provider would be simple. The DB creation script is in the root of the repo. Create that DB, rename `config.example.json` to `config.json`, fill it out, and run.