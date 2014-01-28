require 'srgs'

module MusicGrammar
  include Srgs::DSL

  extend self

  grammar 'topLevel' do
    private_rule 'type' do
      one_of do
        item 'artist'
        item 'song'
        item 'album'
        item 'playlist'
      end
    end

    private_rule 'action' do
      one_of do
        item 'play'
        item 'pause'
      end
    end

    private_rule 'music' do
      item 'play the'
      reference 'type'
      tag 'out.type=rules.type;'
      reference 'grammar:dictation'
      tag 'out.media=rules.latest();'
      item 'on spotify', repeat: "0-1"
    end

    private_rule 'music_control' do
      reference 'action'
      tag 'out.action=rules.action'
    end

    private_rule 'topLevel' do
      item 'Mycroft'
      one_of do
        reference_item 'music'
        reference_item 'music_control'
      end
    end
  end
end