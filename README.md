# Tasks

Play a multiplayer game as normal (everyone needs the mod) and you'll get some tasks to complete each stage. You're competing vs the other players to be the first to complete a task.

The tasks aren't especially difficult to do. The goal is to race your friends to be the first to complete them.

The mod technically works in singleplayer, but most of the tasks are only active in multiplayer so you'll have a much smaller pool.

## Task Examples

AirKills - Jump, then get x kills before you touch the ground.

MostDistance - Travel the farthest. Evaluated when you charge the TP and use it to go to the next stage.

OpenChests - Be the first to open x chests (multishops count too).

Die - It's a race to 0hp.

VeryBest - Be the very best. Like no one ever was. (kill the most unique enemies. Beetle and fire beetle are different.)

There are 20+ tasks to do. The description mostly tells you what to do. Some of the descriptions are a bit vague so you'll have to figure out what to do.

## Rewards

Completing a task rewards you the item shown in the UI. 

If the UI says you will get 5 of an item, they are temp items. You'll lose 3 of them at the end of the stage and lose 2 more at the end of next stage.

If the UI has a picture of the command artifact, you'll get a command droplet of that item's colour. 

## UI

![effects](https://i.imgur.com/bUrLYQD.png)

The green bar is your progress. The gold bar is your closest rival if you're in the lead. The blue bar is if someone else is winning. You always see your own progress as green and 1 other person's progress as another bar.

I modeled the bars and colours after how barriers and shields work in game (golden barriers overlap your hp bar to the left and blue shields are on the right), but it's not as intuitive as I would have liked.

Basically, the progress UI is bad, but serviceable. (except it only shows your own progres if you are the host for some reason).

Command Icon (purple thing by goat hoof) - shows when the reward will be a command droplet. So in this case, instead of getting a hoof, you get a white command droplet. It drops to the ground and then it is free game so be quick!

x5 - that item will be a temp item. So, the armour and syringes here.

## Installation

Requires BepInEx, R2API, HookGenPatcher, and MiniRpcLib

Unzip and place Tasks.dll in Risk of Rain 2\BepInEx\plugins

## Config

The first time you launch, the mod makes a config file in: Risk of Rain 2\BepInEx\config named com.Solrun.Tasks.cfg. If you mess up the numbers too much, delete the file and a new default one will be created when you launch the game next.

The mod uses the host's config file. Everyone else's is ignored. Lines starting with # are comments so they don't do anything.

You can edit stuff in there to change the frequency of certain tasks individually. Set a task to 0 if you don't like it and it won't show up.

You can increase how many tasks are active each stage, but I don't really recommend it. Fewer tasks means you have to compete for them. Too many tasks and you can't remember which ones are active at any one time.

## Changelog

1.0.0 - the mod probably works. The UI is bad and there are probably bugs.

1.1.0 - Updated to work with SoTV Update
